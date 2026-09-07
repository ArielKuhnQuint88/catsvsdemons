using System.Collections.Generic;
using CatsVsDemons.CameraSystem;
using UnityEngine;

namespace CatsVsDemons.Waves
{
    [DefaultExecutionOrder(-100)]
    public sealed class PhaseEnvironmentController : MonoBehaviour
    {
        private const float SurfaceWidth = 2.45f;
        private const float BorderWidth = 2.85f;
        private const float SurfaceHeight = 0.12f;
        private const float BorderHeight = 0.025f;
        private const float ViewportMargin = 0.08f;
        private const float EntrancePadding = 3f;
        private const float MaximumEntranceExtension = 48f;
        private const float EntranceBlend = 0.52f;
        private readonly Dictionary<Transform, Vector3[]> basePaths = new();
        private readonly Dictionary<Transform, Vector3> baseBuildSpots = new();
        private readonly Dictionary<Renderer, Color> baseColors = new();
        private readonly Dictionary<Transform, Vector3[]> laidOutPaths = new();
        private readonly Dictionary<Color32, Material> runtimeMaterials = new();

        private EnemyWaveSpawner spawner;
        private Transform pathsRoot;
        private Transform buildSpotsRoot;
        private CameraModeController cameraController;
        private Transform runtimeGardenRoot;
        private Transform pathBorderRoot;
        private Transform restoredFlowerRoot;
        private Transform scenarioLandmarkRoot;
        private bool captured;
        private bool gardenLandmarksRestored;
        private int pendingPathRefreshPhase;
        private int landmarkScenario = -1;
        private int landmarkSeason = -1;

        private void Awake()
        {
            pathsRoot = GameObject.Find("Game/Paths")?.transform;
            buildSpotsRoot = GameObject.Find("Game/BuildSpots")?.transform;
            cameraController = Object.FindFirstObjectByType<CameraModeController>();
            CaptureInitialState();
        }

        private void Start()
        {
            spawner = Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("PhaseEnvironmentController: WaveSpawner not found.", this);
                return;
            }

            spawner.PhaseStarted += ApplyPhase;
            int initialPhase = spawner.CurrentPhase > 0
                ? spawner.CurrentPhase
                : spawner.StartingPhase;
            ApplyPhase(Mathf.Max(1, initialPhase), spawner.TotalPhases);
        }

        private void OnDestroy()
        {
            if (spawner != null)
            {
                spawner.PhaseStarted -= ApplyPhase;
            }
        }

        private void CaptureInitialState()
        {
            if (captured)
            {
                return;
            }

            if (pathsRoot != null)
            {
                foreach (Transform path in pathsRoot)
                {
                    List<Transform> joints = GetJoints(path);
                    Vector3[] positions = new Vector3[joints.Count];
                    for (int index = 0; index < joints.Count; index++)
                    {
                        positions[index] = joints[index].localPosition;
                    }
                    basePaths[path] = positions;
                }
            }

            if (buildSpotsRoot != null)
            {
                foreach (Transform spot in buildSpotsRoot)
                {
                    baseBuildSpots[spot] = spot.localPosition;
                }
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial != null)
                {
                    baseColors[renderer] = renderer.sharedMaterial.color;
                }
            }

            captured = true;
        }

        public void ApplyPhase(int phase, int totalPhases)
        {
            CaptureInitialState();
            phase = Mathf.Clamp(phase, 1, Mathf.Max(1, totalPhases));

            if (cameraController == null)
            {
                cameraController =
                    Object.FindFirstObjectByType<CameraModeController>();
            }
            cameraController?.SetPhaseZoom(phase);

            ApplyPathLayout(phase);
            ApplyBuildSpotLayout(phase);
            RestoreGardenLandmarks();
            ApplyGardenTheme(phase);
            pendingPathRefreshPhase = phase;

            string title = CampaignProgress.GetPhaseTitle(phase);

            Debug.Log($"Phase {phase}: {title} loaded.", this);
        }

        private void ApplyPathLayout(int phase)
        {
            int scenario = CampaignProgress.GetScenarioIndex(phase);
            int season = CampaignProgress.GetSeasonIndex(phase);
            float minimumExtension = 12f + scenario * 3f + season * 1.2f;
            float waveAmplitude = season * 0.12f + scenario * 0.18f;
            float waveCycles = season < 2
                ? 0f
                : 1.35f + season * 0.45f + scenario * 0.35f;
            int pathIndex = 0;
            laidOutPaths.Clear();

            foreach (KeyValuePair<Transform, Vector3[]> entry in basePaths)
            {
                Transform path = entry.Key;
                if (path == null)
                {
                    continue;
                }

                Vector3[] baseline = entry.Value;
                if (baseline.Length == 0)
                {
                    continue;
                }

                Vector3[] extended = new Vector3[baseline.Length];
                List<Transform> joints = GetJoints(path);

                Vector3 outwardDirection = GetOutwardDirection(baseline);
                float startExtension = GetEntranceExtension(
                    path,
                    baseline[0],
                    outwardDirection,
                    minimumExtension
                );

                for (int index = 0; index < baseline.Length; index++)
                {
                    float t = baseline.Length <= 1
                        ? 0f
                        : index / (float)(baseline.Length - 1);

                    float entranceProgress = Mathf.Clamp01(t / EntranceBlend);
                    float entranceWeight =
                        1f - Mathf.SmoothStep(0f, 1f, entranceProgress);
                    Vector3 extension =
                        outwardDirection * startExtension * entranceWeight;

                    Vector3 previous = baseline[Mathf.Max(0, index - 1)];
                    Vector3 next = baseline[Mathf.Min(baseline.Length - 1, index + 1)];
                    Vector3 tangent = next - previous;
                    tangent.y = 0f;
                    if (tangent.sqrMagnitude < 0.0001f)
                    {
                        tangent = Vector3.forward;
                    }
                    tangent.Normalize();

                    Vector3 side = Vector3.Cross(Vector3.up, tangent);
                    float entranceArc = Mathf.Sin(entranceProgress * Mathf.PI);
                    float entranceArcAmplitude = Mathf.Min(
                        4.2f,
                        startExtension * 0.16f
                    );
                    float entranceArcDirection = GetEntranceArcDirection(
                        path.name,
                        pathIndex
                    );
                    float centerEnvelope = Mathf.Sin(t * Mathf.PI);
                    float houseProtection =
                        1f - Mathf.SmoothStep(0.55f, 0.82f, t);
                    float wave = Mathf.Sin(
                        (t * waveCycles * Mathf.PI * 2f) + pathIndex * 0.83f
                    );

                    Vector3 position =
                        baseline[index] +
                        extension +
                        side * entranceArc * entranceArcAmplitude *
                        entranceArcDirection +
                        side * wave * waveAmplitude *
                        centerEnvelope * houseProtection;

                    position.x = Mathf.Clamp(position.x, -72f, 72f);
                    position.z = Mathf.Clamp(position.z, -56f, 56f);
                    extended[index] = position;

                    if (index < joints.Count)
                    {
                        joints[index].localPosition = position;
                    }
                }

                laidOutPaths[path] = extended;

                UpdateLegacySegments(path, extended);
                UpdateRibbon(
                    path,
                    path.name + "_Border",
                    extended,
                    BorderWidth,
                    BorderHeight,
                    false
                );
                UpdateRibbon(
                    path,
                    path.name + "_Surface",
                    extended,
                    SurfaceWidth,
                    SurfaceHeight,
                    true
                );
                pathIndex++;
            }

            RebuildPathBorders();
        }

        private void LateUpdate()
        {
            if (pendingPathRefreshPhase <= 0)
            {
                return;
            }

            int phase = pendingPathRefreshPhase;
            pendingPathRefreshPhase = 0;

            cameraController?.SetPhaseZoom(phase);
            ApplyPathLayout(phase);
            ApplyGardenTheme(phase);
        }

        private static Vector3 GetOutwardDirection(Vector3[] baseline)
        {
            Vector3 direction = baseline[0] - baseline[baseline.Length - 1];
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector3.back;
            }

            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.z)
                ? new Vector3(Mathf.Sign(direction.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(direction.z));
        }

        private static float GetEntranceArcDirection(string pathName, int pathIndex)
        {
            if (pathName.Contains("Left"))
            {
                return -1f;
            }

            if (pathName.Contains("Right"))
            {
                return 1f;
            }

            if (pathName.Contains("Bottom"))
            {
                return -1f;
            }

            return pathIndex % 2 == 0 ? -1f : 1f;
        }

        private static float GetEntranceExtension(
            Transform path,
            Vector3 entrance,
            Vector3 outwardDirection,
            float minimumExtension)
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null || !gameCamera.isActiveAndEnabled)
            {
                return minimumExtension;
            }

            float extension = minimumExtension;
            while (extension < MaximumEntranceExtension)
            {
                Vector3 candidate = path.TransformPoint(
                    entrance + outwardDirection * extension
                );
                Vector3 viewport = gameCamera.WorldToViewportPoint(candidate);

                bool insidePaddedViewport =
                    viewport.z > 0f &&
                    viewport.x >= -ViewportMargin &&
                    viewport.x <= 1f + ViewportMargin &&
                    viewport.y >= -ViewportMargin &&
                    viewport.y <= 1f + ViewportMargin;

                if (!insidePaddedViewport)
                {
                    break;
                }

                extension += 1f;
            }

            return Mathf.Min(
                MaximumEntranceExtension,
                extension + EntrancePadding
            );
        }

        private void RebuildPathBorders()
        {
            pathBorderRoot ??= GetRuntimeGardenChild("PathBorders");
            // Old versions decorated every route with generated pebbles. The
            // paths now stay visually clean so the scenario landmarks stand out.
            ClearChildren(pathBorderRoot);
        }

        private void RestoreGardenLandmarks()
        {
            if (gardenLandmarksRestored)
            {
                return;
            }

            Transform garden = GameObject.Find("Game/Environment/JapaneseGarden3D")?.transform;
            bool hasPond = false;
            bool hasBridge = false;
            bool hasFlowers = false;

            if (garden != null)
            {
                garden.gameObject.SetActive(true);
                hasPond = RevealGardenElements(garden, "KoiPond");
                hasBridge = RevealGardenElements(garden, "Bridge");
                hasFlowers = RevealGardenElements(garden, "FlowerPatch");
            }

            Transform landmarkRoot = GetRuntimeGardenChild("RestoredLandmarks");
            if (!hasPond)
            {
                CreateFallbackPond(landmarkRoot, new Vector3(-15f, 0f, -9.5f), 0f);
                CreateFallbackPond(landmarkRoot, new Vector3(15f, 0f, 9.5f), 180f);
                hasBridge = true;
            }
            else if (!hasBridge)
            {
                CreateFallbackBridge(
                    landmarkRoot,
                    new Vector3(-15f, 0f, -9.5f),
                    0f
                );
            }

            if (!hasFlowers)
            {
                restoredFlowerRoot ??= GetRuntimeGardenChild("RestoredFlowers");
                ClearChildren(restoredFlowerRoot);
                CreateLegacyFlowerBeds(restoredFlowerRoot);
            }

            gardenLandmarksRestored = true;
        }

        private static bool RevealGardenElements(Transform root, string elementName)
        {
            bool found = false;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != elementName)
                {
                    continue;
                }

                candidate.gameObject.SetActive(true);
                foreach (Renderer renderer in candidate.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = true;
                }
                found = true;
            }
            return found;
        }

        private void CreateLegacyFlowerBeds(Transform parent)
        {
            Vector3[] patches =
            {
                new Vector3(-10.5f, 0f, 6.8f),
                new Vector3(10.5f, 0f, -6.8f),
                new Vector3(-5.5f, 0f, 10.2f),
                new Vector3(6.2f, 0f, -10.4f),
                new Vector3(-16.5f, 0f, 4.5f),
                new Vector3(16.2f, 0f, -3.8f)
            };
            Color[] colors =
            {
                new Color(1f, 0.38f, 0.62f),
                new Color(0.78f, 0.38f, 0.95f),
                new Color(1f, 0.78f, 0.14f),
                new Color(0.95f, 0.95f, 0.88f)
            };

            for (int index = 0; index < patches.Length; index++)
            {
                CreateFlowerPatch(parent, patches[index], colors[index % colors.Length]);
            }
        }

        private void CreateFlowerPatch(Transform parent, Vector3 worldPosition, Color petalColor)
        {
            GameObject patch = new GameObject("FlowerPatch");
            patch.transform.SetParent(parent, false);
            patch.transform.position = worldPosition;

            CreateRuntimePart(
                "FlowerBed",
                PrimitiveType.Sphere,
                patch.transform,
                new Vector3(0f, 0.08f, 0f),
                new Vector3(1.35f, 0.16f, 0.9f),
                new Color(0.09f, 0.30f, 0.10f),
                Vector3.zero
            );

            for (int index = 0; index < 10; index++)
            {
                float angle = index * 2.399f;
                float radius = 0.25f + (index % 4) * 0.22f;
                CreateFlower(
                    patch.transform,
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    ),
                    index % 3 == 0 ? new Color(1f, 0.9f, 0.28f) : petalColor,
                    0.42f + (index % 2) * 0.07f
                );
            }
        }

        private void CreateFlower(
            Transform parent,
            Vector3 position,
            Color petalColor,
            float size
        )
        {
            GameObject flower = new GameObject("Flower");
            flower.transform.SetParent(parent, false);
            flower.transform.localPosition = position;

            CreateRuntimePart(
                "Stem",
                PrimitiveType.Cylinder,
                flower.transform,
                new Vector3(0f, size * 0.6f, 0f),
                new Vector3(size * 0.08f, size * 0.6f, size * 0.08f),
                new Color(0.10f, 0.42f, 0.12f),
                Vector3.zero
            );

            for (int petal = 0; petal < 5; petal++)
            {
                float angle = petal * Mathf.PI * 2f / 5f;
                CreateRuntimePart(
                    "Petal",
                    PrimitiveType.Sphere,
                    flower.transform,
                    new Vector3(
                        Mathf.Cos(angle) * size * 0.32f,
                        size * 1.25f,
                        Mathf.Sin(angle) * size * 0.32f
                    ),
                    new Vector3(size * 0.28f, size * 0.10f, size * 0.2f),
                    petalColor,
                    new Vector3(0f, -petal * 72f, 0f)
                );
            }

            CreateRuntimePart(
                "FlowerCenter",
                PrimitiveType.Sphere,
                flower.transform,
                new Vector3(0f, size * 1.27f, 0f),
                Vector3.one * size * 0.22f,
                new Color(1f, 0.72f, 0.05f),
                Vector3.zero
            );
        }

        private void CreateFallbackPond(Transform parent, Vector3 worldPosition, float yaw)
        {
            GameObject pond = new GameObject("KoiPond");
            pond.transform.SetParent(parent, false);
            pond.transform.position = worldPosition;
            pond.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            CreateRuntimePart(
                "PondBed",
                PrimitiveType.Cylinder,
                pond.transform,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(4.75f, 0.12f, 3.15f),
                new Color(0.46f, 0.38f, 0.27f),
                Vector3.zero
            );
            CreateRuntimePart(
                "Water",
                PrimitiveType.Cylinder,
                pond.transform,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(4.35f, 0.08f, 2.75f),
                new Color(0.025f, 0.23f, 0.32f),
                Vector3.zero
            );
            CreateRuntimePart(
                "WaterHighlight",
                PrimitiveType.Cylinder,
                pond.transform,
                new Vector3(-0.18f, 0.10f, 0.12f),
                new Vector3(4.02f, 0.018f, 2.48f),
                new Color(0.06f, 0.52f, 0.62f),
                Vector3.zero
            );

            for (int index = 0; index < 16; index++)
            {
                float angle = index * Mathf.PI * 2f / 16f;
                float size = 0.48f + (index % 3) * 0.08f;
                CreateRuntimePart(
                    "PondRock",
                    PrimitiveType.Sphere,
                    pond.transform,
                    new Vector3(
                        Mathf.Cos(angle) * 4.42f,
                        0.17f,
                        Mathf.Sin(angle) * 2.85f
                    ),
                    new Vector3(size, 0.28f, size * 0.82f),
                    index % 2 == 0
                        ? new Color(0.25f, 0.29f, 0.27f)
                        : new Color(0.36f, 0.37f, 0.32f),
                    new Vector3(0f, index * 29f, 0f)
                );
            }

            CreateFallbackBridge(pond.transform, Vector3.zero, 0f, true);
        }

        private void CreateFallbackBridge(
            Transform parent,
            Vector3 worldPosition,
            float yaw,
            bool useLocalPosition = false
        )
        {
            GameObject bridge = new GameObject("Bridge");
            bridge.transform.SetParent(parent, false);
            if (useLocalPosition)
            {
                bridge.transform.localPosition = worldPosition;
                bridge.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
            else
            {
                bridge.transform.position = worldPosition;
                bridge.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            Color bridgeRed = new Color(0.58f, 0.055f, 0.03f);
            Color bridgeDark = new Color(0.18f, 0.055f, 0.035f);
            for (int plank = -4; plank <= 4; plank++)
            {
                float arch = 0.48f + (1f - Mathf.Abs(plank) / 5f) * 0.35f;
                CreateRuntimePart(
                    "BridgePlank",
                    PrimitiveType.Cube,
                    bridge.transform,
                    new Vector3(0f, arch, plank * 0.57f),
                    new Vector3(1.35f, 0.14f, 0.5f),
                    bridgeRed,
                    new Vector3(plank * -1.4f, 0f, 0f)
                );
            }

            foreach (float side in new[] { -0.82f, 0.82f })
            {
                CreateRuntimePart(
                    "BridgeRail",
                    PrimitiveType.Cube,
                    bridge.transform,
                    new Vector3(side, 1.25f, 0f),
                    new Vector3(0.09f, 0.09f, 5.2f),
                    bridgeDark,
                    new Vector3(0f, 0f, side * -5f)
                );

                for (int post = -2; post <= 2; post++)
                {
                    CreateRuntimePart(
                        "BridgePost",
                        PrimitiveType.Cylinder,
                        bridge.transform,
                        new Vector3(side, 0.92f, post * 1.15f),
                        new Vector3(0.10f, 0.55f, 0.10f),
                        bridgeDark,
                        Vector3.zero
                    );
                }
            }
        }

        private Transform GetRuntimeGardenChild(string childName)
        {
            runtimeGardenRoot ??= GetRuntimeGardenRoot();
            Transform child = runtimeGardenRoot.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(runtimeGardenRoot, false);
            return childObject.transform;
        }

        private Transform GetRuntimeGardenRoot()
        {
            Transform gameRoot = GameObject.Find("Game")?.transform ?? transform;
            Transform existing = gameRoot.Find("RuntimeGarden");
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("RuntimeGarden");
            root.transform.SetParent(gameRoot, false);
            return root.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private GameObject CreateRuntimePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 scale,
            Color color,
            Vector3 rotation
        )
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(rotation);
            part.transform.localScale = scale;
            DisableCollider(part);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetRuntimeMaterial(color);
            }
            return part;
        }

        private GameObject CreateRuntimeWorldPart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 worldPosition,
            Vector3 scale,
            Color color,
            Vector3 rotation
        )
        {
            GameObject part = CreateRuntimePart(
                name,
                primitive,
                parent,
                Vector3.zero,
                scale,
                color,
                rotation
            );
            part.transform.position = worldPosition;
            return part;
        }

        private static void DisableCollider(GameObject part)
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            collider.enabled = false;
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private Material GetRuntimeMaterial(Color color)
        {
            Color32 key = color;
            if (runtimeMaterials.TryGetValue(key, out Material material))
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            material = shader == null ? null : new Material(shader)
            {
                color = color
            };
            runtimeMaterials[key] = material;
            return material;
        }

        private static void UpdateLegacySegments(
            Transform path,
            Vector3[] points)
        {
            List<Transform> segments = new();
            foreach (Transform child in path)
            {
                if (child.name.StartsWith("Segment_"))
                {
                    segments.Add(child);
                }
            }

            segments.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            int segmentCount = Mathf.Min(segments.Count, points.Length - 1);

            for (int index = 0; index < segmentCount; index++)
            {
                Transform segment = segments[index];
                Vector3 start = points[index];
                Vector3 end = points[index + 1];
                Vector3 direction = end - start;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                Vector3 center = (start + end) * 0.5f;
                center.y = segment.localPosition.y;
                segment.localPosition = center;
                segment.localRotation = Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );

                Vector3 scale = segment.localScale;
                scale.z = direction.magnitude;
                segment.localScale = scale;
            }
        }

        private void ApplyBuildSpotLayout(int phase)
        {
            int scenario = CampaignProgress.GetScenarioIndex(phase);
            int season = CampaignProgress.GetSeasonIndex(phase);
            float radialScale = 1f + scenario * 0.06f + season * 0.015f;
            foreach (KeyValuePair<Transform, Vector3> entry in baseBuildSpots)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                Vector3 position = entry.Value;
                position.x *= radialScale;
                position.z *= radialScale;
                entry.Key.localPosition = position;
            }
        }

        private void ApplyGardenTheme(int phase)
        {
            int scenario = CampaignProgress.GetScenarioIndex(phase);
            int season = CampaignProgress.GetSeasonIndex(phase);
            Color foliage = GetFoliageColor(scenario, season);
            Color petals = GetPetalColor(scenario, season);
            Color water = GetWaterColor(scenario, season);
            Color lantern = GetLanternColor(scenario, season);
            Color ground = GetGroundColor(scenario, season);

            foreach (KeyValuePair<Renderer, Color> entry in baseColors)
            {
                Renderer renderer = entry.Key;
                if (renderer == null)
                {
                    continue;
                }

                Color color = entry.Value;
                string objectName = renderer.gameObject.name;

                if (ContainsAny(objectName, "Blossoms", "MapleCrown"))
                {
                    color = foliage;
                }
                else if (ContainsAny(objectName, "Petal"))
                {
                    color = petals;
                }
                else if (ContainsAny(objectName, "Water", "DeepWater"))
                {
                    color = water;
                }
                else if (ContainsAny(objectName, "LanternLight"))
                {
                    color = lantern;
                }
                else if (ContainsAny(objectName, "GardenGrass", "RakedSand"))
                {
                    color = ground;
                }
                else if (scenario == 1 &&
                    ContainsAny(objectName, "BambooLeaves", "BambooStem"))
                {
                    color = foliage;
                }
                else if (scenario == 2 &&
                    ContainsAny(objectName, "ZenStone", "PondRock", "ShoreRock"))
                {
                    color = Color.Lerp(color, new Color(0.30f, 0.24f, 0.42f), 0.5f);
                }

                renderer.material.color = color;
            }

            ApplyRuntimeFlowerTheme(petals);
            RebuildScenarioLandmarks(scenario, season);
            ApplyAtmosphere(scenario, season, ground);
        }

        private void ApplyRuntimeFlowerTheme(Color petals)
        {
            if (restoredFlowerRoot == null)
            {
                return;
            }

            foreach (Renderer renderer in
                restoredFlowerRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == "Petal")
                {
                    renderer.sharedMaterial = GetRuntimeMaterial(petals);
                }
                else if (renderer.gameObject.name == "FlowerCenter")
                {
                    renderer.sharedMaterial = GetRuntimeMaterial(
                        Color.Lerp(petals, new Color(1f, 0.78f, 0.10f), 0.55f)
                    );
                }
            }
        }

        private void RebuildScenarioLandmarks(int scenario, int season)
        {
            scenarioLandmarkRoot ??=
                GetRuntimeGardenChild("ScenarioLandmarks");
            if (landmarkScenario == scenario && landmarkSeason == season &&
                scenarioLandmarkRoot.childCount > 0)
            {
                return;
            }

            ClearChildren(scenarioLandmarkRoot);

            Color foliage = GetFoliageColor(scenario, season);
            Color petals = GetPetalColor(scenario, season);
            Color water = GetWaterColor(scenario, season);
            Color accent = GetAccentColor(scenario, season);
            Color stone = GetStoneColor(scenario, season);

            switch (scenario)
            {
                case 0:
                    CreateSakuraGardenLandmarks(
                        scenarioLandmarkRoot,
                        season,
                        foliage,
                        petals,
                        accent,
                        stone
                    );
                    break;
                case 1:
                    CreateBambooGroveLandmarks(
                        scenarioLandmarkRoot,
                        season,
                        foliage,
                        water,
                        accent,
                        stone
                    );
                    break;
                default:
                    CreateEclipseSanctuaryLandmarks(
                        scenarioLandmarkRoot,
                        season,
                        foliage,
                        accent,
                        stone
                    );
                    break;
            }

            landmarkScenario = scenario;
            landmarkSeason = season;
        }

        private void CreateSakuraGardenLandmarks(
            Transform parent,
            int season,
            Color foliage,
            Color petals,
            Color accent,
            Color stone)
        {
            Color bark = season == 3
                ? new Color(0.34f, 0.37f, 0.42f)
                : new Color(0.26f, 0.12f, 0.08f);
            Vector3[] treePositions =
            {
                new Vector3(-27f, 0f, 19f),
                new Vector3(27f, 0f, 19f),
                new Vector3(-27f, 0f, -18f),
                new Vector3(27f, 0f, -18f)
            };

            for (int index = 0; index < treePositions.Length; index++)
            {
                CreateSakuraTree(
                    parent,
                    treePositions[index],
                    0.96f + (index % 2) * 0.12f,
                    foliage,
                    bark,
                    index * 31f
                );
            }

            CreateToriiGate(
                parent,
                new Vector3(0f, 0f, 27f),
                0f,
                1.24f,
                new Color(0.66f, 0.055f, 0.035f),
                accent
            );
            CreateGardenLantern(parent, new Vector3(-19f, 0f, 4f), stone, accent);
            CreateGardenLantern(parent, new Vector3(19f, 0f, -4f), stone, accent);

            CreateSeasonalAccents(
                parent,
                season,
                new[]
                {
                    new Vector3(-21f, 0f, 13f),
                    new Vector3(21f, 0f, 13f),
                    new Vector3(-21f, 0f, -13f),
                    new Vector3(21f, 0f, -13f)
                },
                petals,
                accent
            );
        }

        private void CreateBambooGroveLandmarks(
            Transform parent,
            int season,
            Color foliage,
            Color water,
            Color accent,
            Color stone)
        {
            Vector3[] grovePositions =
            {
                new Vector3(-28f, 0f, 19f),
                new Vector3(28f, 0f, 19f),
                new Vector3(-28f, 0f, -18f),
                new Vector3(28f, 0f, -18f)
            };

            for (int index = 0; index < grovePositions.Length; index++)
            {
                CreateBambooCluster(
                    parent,
                    grovePositions[index],
                    0.95f + (index % 2) * 0.13f,
                    foliage,
                    accent,
                    index * 41f
                );
            }

            CreateStream(parent, new Vector3(23f, 0f, 1.5f), 0f, water, stone);
            CreateBambooBridge(
                parent,
                new Vector3(23f, 0f, 1.5f),
                0f,
                new Color(0.46f, 0.22f, 0.06f),
                accent
            );
            CreateStoneGuardian(
                parent,
                new Vector3(-24f, 0f, 1.5f),
                90f,
                stone,
                accent
            );

            CreateSeasonalAccents(
                parent,
                season,
                new[]
                {
                    new Vector3(-21f, 0f, 11f),
                    new Vector3(20f, 0f, 12f),
                    new Vector3(-20f, 0f, -12f),
                    new Vector3(20f, 0f, -12f)
                },
                foliage,
                accent
            );
        }

        private void CreateEclipseSanctuaryLandmarks(
            Transform parent,
            int season,
            Color foliage,
            Color accent,
            Color stone)
        {
            Vector3[] obeliskPositions =
            {
                new Vector3(-27f, 0f, 18f),
                new Vector3(27f, 0f, 18f),
                new Vector3(-27f, 0f, -17f),
                new Vector3(27f, 0f, -17f)
            };

            for (int index = 0; index < obeliskPositions.Length; index++)
            {
                CreateEclipseObelisk(
                    parent,
                    obeliskPositions[index],
                    index * 37f,
                    stone,
                    accent
                );
            }

            CreateToriiGate(
                parent,
                new Vector3(0f, 0f, 27f),
                0f,
                1.32f,
                new Color(0.08f, 0.035f, 0.14f),
                accent
            );
            CreateRitualCircle(
                parent,
                new Vector3(0f, 0f, -24f),
                stone,
                accent
            );
            CreateEclipseBrazier(parent, new Vector3(-20f, 0f, 3f), stone, accent);
            CreateEclipseBrazier(parent, new Vector3(20f, 0f, -3f), stone, accent);

            CreateSeasonalAccents(
                parent,
                season,
                new[]
                {
                    new Vector3(-20f, 0f, 12f),
                    new Vector3(20f, 0f, 12f),
                    new Vector3(-20f, 0f, -12f),
                    new Vector3(20f, 0f, -12f)
                },
                foliage,
                accent
            );
        }

        private void CreateSakuraTree(
            Transform parent,
            Vector3 basePosition,
            float scale,
            Color foliage,
            Color bark,
            float yaw)
        {
            float height = 5.2f * scale;
            CreateRuntimeWorldPart(
                "SakuraTrunk",
                PrimitiveType.Cylinder,
                parent,
                basePosition + Vector3.up * (height * 0.5f),
                new Vector3(0.42f * scale, height * 0.5f, 0.42f * scale),
                bark,
                new Vector3(0f, yaw, 0f)
            );

            for (int branch = 0; branch < 3; branch++)
            {
                float branchYaw = yaw + branch * 120f + 20f;
                Vector3 direction = Quaternion.Euler(0f, branchYaw, 0f) *
                    Vector3.forward;
                CreateRuntimeWorldPart(
                    "SakuraBranch",
                    PrimitiveType.Cube,
                    parent,
                    basePosition + Vector3.up * (height * 0.68f) +
                    direction * (0.92f * scale),
                    new Vector3(0.22f * scale, 0.18f * scale, 2.15f * scale),
                    bark,
                    new Vector3(18f, branchYaw, branch % 2 == 0 ? 16f : -16f)
                );
            }

            for (int crown = 0; crown < 5; crown++)
            {
                float crownYaw = yaw + crown * 72f;
                Vector3 direction = Quaternion.Euler(0f, crownYaw, 0f) *
                    Vector3.forward;
                float radius = crown == 4 ? 0f : 1.55f * scale;
                float vertical = height * (0.84f + (crown % 2) * 0.08f);
                Color crownColor = Color.Lerp(
                    foliage,
                    Color.white,
                    crown == 4 ? 0.08f : 0.18f
                );
                CreateRuntimeWorldPart(
                    "SakuraCrown",
                    PrimitiveType.Sphere,
                    parent,
                    basePosition + Vector3.up * vertical + direction * radius,
                    new Vector3(2.15f, 1.18f, 1.72f) * scale,
                    crownColor,
                    new Vector3(0f, crownYaw, 0f)
                );
            }
        }

        private void CreateToriiGate(
            Transform parent,
            Vector3 center,
            float yaw,
            float scale,
            Color gateColor,
            Color accent)
        {
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            for (int side = -1; side <= 1; side += 2)
            {
                CreateRuntimeWorldPart(
                    "ToriiPillar",
                    PrimitiveType.Capsule,
                    parent,
                    center + right * side * (2.35f * scale) +
                    Vector3.up * (2.25f * scale),
                    new Vector3(0.27f * scale, 2.25f * scale, 0.27f * scale),
                    gateColor,
                    new Vector3(0f, yaw, 0f)
                );
                CreateRuntimeWorldPart(
                    "ToriiFoot",
                    PrimitiveType.Cylinder,
                    parent,
                    center + right * side * (2.35f * scale) + Vector3.up * 0.12f,
                    new Vector3(0.46f * scale, 0.12f, 0.46f * scale),
                    Color.Lerp(gateColor, Color.black, 0.32f),
                    Vector3.zero
                );
            }

            CreateRuntimeWorldPart(
                "ToriiMainBeam",
                PrimitiveType.Cube,
                parent,
                center + Vector3.up * (4.48f * scale),
                new Vector3(5.9f * scale, 0.22f * scale, 0.48f * scale),
                gateColor,
                new Vector3(0f, yaw, 0f)
            );
            CreateRuntimeWorldPart(
                "ToriiTopBeam",
                PrimitiveType.Cube,
                parent,
                center + Vector3.up * (4.82f * scale),
                new Vector3(6.45f * scale, 0.20f * scale, 0.68f * scale),
                Color.Lerp(gateColor, accent, 0.12f),
                new Vector3(0f, yaw, 0f)
            );
            CreateRuntimeWorldPart(
                "ToriiEmblem",
                PrimitiveType.Sphere,
                parent,
                center + Vector3.up * (4.48f * scale),
                new Vector3(0.34f * scale, 0.34f * scale, 0.34f * scale),
                accent,
                Vector3.zero
            );
        }

        private void CreateGardenLantern(
            Transform parent,
            Vector3 position,
            Color stone,
            Color light)
        {
            CreateRuntimeWorldPart(
                "LandmarkLanternBase",
                PrimitiveType.Cylinder,
                parent,
                position + Vector3.up * 0.16f,
                new Vector3(0.64f, 0.16f, 0.64f),
                stone,
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "LandmarkLanternPost",
                PrimitiveType.Cube,
                parent,
                position + Vector3.up * 0.92f,
                new Vector3(0.22f, 0.76f, 0.22f),
                Color.Lerp(stone, Color.black, 0.2f),
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "LandmarkLanternRoof",
                PrimitiveType.Cylinder,
                parent,
                position + Vector3.up * 1.7f,
                new Vector3(0.78f, 0.15f, 0.78f),
                stone,
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "LandmarkLanternGlow",
                PrimitiveType.Sphere,
                parent,
                position + Vector3.up * 1.38f,
                Vector3.one * 0.24f,
                light,
                Vector3.zero
            );
        }

        private void CreateBambooCluster(
            Transform parent,
            Vector3 center,
            float scale,
            Color foliage,
            Color accent,
            float yaw)
        {
            Color stalk = Color.Lerp(
                new Color(0.08f, 0.25f, 0.10f),
                foliage,
                0.38f
            );
            for (int index = 0; index < 5; index++)
            {
                float angle = yaw + index * 137.5f;
                float radius = 0.45f + (index % 3) * 0.42f;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward * radius * scale;
                float height = (4.5f + (index % 3) * 0.72f) * scale;
                Vector3 basePosition = center + offset;

                CreateRuntimeWorldPart(
                    "GroveBambooStalk",
                    PrimitiveType.Capsule,
                    parent,
                    basePosition + Vector3.up * (height * 0.5f),
                    new Vector3(0.20f * scale, height * 0.5f, 0.20f * scale),
                    stalk,
                    new Vector3(0f, angle, 0f)
                );
                CreateRuntimeWorldPart(
                    "GroveBambooNode",
                    PrimitiveType.Cylinder,
                    parent,
                    basePosition + Vector3.up * (height * 0.62f),
                    new Vector3(0.24f * scale, 0.055f * scale, 0.24f * scale),
                    accent,
                    Vector3.zero
                );
                CreateRuntimeWorldPart(
                    "GroveBambooLeaves",
                    PrimitiveType.Sphere,
                    parent,
                    basePosition + Vector3.up * (height + 0.12f * scale),
                    new Vector3(1.12f, 0.34f, 0.86f) * scale,
                    foliage,
                    new Vector3(0f, angle, 0f)
                );
            }
        }

        private void CreateStream(
            Transform parent,
            Vector3 center,
            float yaw,
            Color water,
            Color stone)
        {
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            for (int index = -1; index <= 1; index++)
            {
                CreateRuntimeWorldPart(
                    "GroveStream",
                    PrimitiveType.Cube,
                    parent,
                    center + forward * (index * 3.3f) + Vector3.up * 0.03f,
                    new Vector3(2.4f, 0.06f, 3.65f),
                    water,
                    new Vector3(0f, yaw + index * 5f, 0f)
                );
            }

            for (int rock = 0; rock < 6; rock++)
            {
                float angle = rock * 60f;
                Vector3 side = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                CreateRuntimeWorldPart(
                    "GroveStreamRock",
                    PrimitiveType.Sphere,
                    parent,
                    center + side * (2.45f + (rock % 2) * 0.42f) +
                    forward * ((rock - 2.5f) * 0.85f) + Vector3.up * 0.16f,
                    new Vector3(0.52f, 0.24f, 0.42f),
                    stone,
                    new Vector3(0f, angle, 0f)
                );
            }
        }

        private void CreateBambooBridge(
            Transform parent,
            Vector3 center,
            float yaw,
            Color wood,
            Color accent)
        {
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            for (int plank = -4; plank <= 4; plank++)
            {
                float arch = 0.36f +
                    (1f - Mathf.Abs(plank) / 5f) * 0.34f;
                CreateRuntimeWorldPart(
                    "GroveBridgePlank",
                    PrimitiveType.Cube,
                    parent,
                    center + forward * (plank * 0.46f) + Vector3.up * arch,
                    new Vector3(1.38f, 0.12f, 0.39f),
                    wood,
                    new Vector3(plank * -1.2f, yaw, 0f)
                );
            }

            for (int sideIndex = -1; sideIndex <= 1; sideIndex += 2)
            {
                Vector3 side = right * sideIndex;
                CreateRuntimeWorldPart(
                    "GroveBridgeRail",
                    PrimitiveType.Cube,
                    parent,
                    center + side * 0.84f + Vector3.up * 1.16f,
                    new Vector3(0.08f, 0.08f, 4.62f),
                    Color.Lerp(wood, accent, 0.16f),
                    new Vector3(0f, yaw, 0f)
                );

                for (int post = -2; post <= 2; post += 2)
                {
                    CreateRuntimeWorldPart(
                        "GroveBridgePost",
                        PrimitiveType.Cylinder,
                        parent,
                        center + side * 0.84f + forward * (post * 0.78f) +
                        Vector3.up * 0.78f,
                        new Vector3(0.10f, 0.64f, 0.10f),
                        wood,
                        Vector3.zero
                    );
                }
            }
        }

        private void CreateStoneGuardian(
            Transform parent,
            Vector3 position,
            float yaw,
            Color stone,
            Color accent)
        {
            CreateRuntimeWorldPart(
                "GroveGuardianBase",
                PrimitiveType.Cylinder,
                parent,
                position + Vector3.up * 0.18f,
                new Vector3(0.82f, 0.18f, 0.82f),
                stone,
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "GroveGuardianBody",
                PrimitiveType.Cube,
                parent,
                position + Vector3.up * 0.92f,
                new Vector3(0.76f, 0.72f, 0.52f),
                stone,
                new Vector3(0f, yaw, 0f)
            );
            CreateRuntimeWorldPart(
                "GroveGuardianHead",
                PrimitiveType.Sphere,
                parent,
                position + Vector3.up * 1.72f,
                new Vector3(0.62f, 0.48f, 0.56f),
                Color.Lerp(stone, Color.white, 0.1f),
                new Vector3(0f, yaw, 0f)
            );
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            CreateRuntimeWorldPart(
                "GroveGuardianEye",
                PrimitiveType.Sphere,
                parent,
                position + Vector3.up * 1.76f + forward * 0.46f,
                Vector3.one * 0.11f,
                accent,
                Vector3.zero
            );
        }

        private void CreateEclipseObelisk(
            Transform parent,
            Vector3 position,
            float yaw,
            Color stone,
            Color accent)
        {
            CreateRuntimeWorldPart(
                "EclipseObeliskBase",
                PrimitiveType.Cylinder,
                parent,
                position + Vector3.up * 0.18f,
                new Vector3(0.96f, 0.18f, 0.96f),
                Color.Lerp(stone, Color.black, 0.26f),
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "EclipseObelisk",
                PrimitiveType.Cylinder,
                parent,
                position + Vector3.up * 2.1f,
                new Vector3(0.46f, 1.92f, 0.46f),
                stone,
                new Vector3(8f, yaw, 7f)
            );
            CreateRuntimeWorldPart(
                "EclipseObeliskCore",
                PrimitiveType.Sphere,
                parent,
                position + Vector3.up * 3.28f,
                Vector3.one * 0.36f,
                accent,
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "EclipseObeliskCrown",
                PrimitiveType.Capsule,
                parent,
                position + Vector3.up * 4.28f,
                new Vector3(0.23f, 0.78f, 0.23f),
                Color.Lerp(accent, Color.white, 0.16f),
                new Vector3(18f, yaw, 18f)
            );
        }

        private void CreateRitualCircle(
            Transform parent,
            Vector3 center,
            Color stone,
            Color accent)
        {
            CreateRuntimeWorldPart(
                "EclipseRitualDais",
                PrimitiveType.Cylinder,
                parent,
                center + Vector3.up * 0.11f,
                new Vector3(3.5f, 0.11f, 3.5f),
                Color.Lerp(stone, Color.black, 0.38f),
                Vector3.zero
            );

            for (int rune = 0; rune < 8; rune++)
            {
                float angle = rune * 45f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward;
                CreateRuntimeWorldPart(
                    "EclipseRuneStone",
                    PrimitiveType.Cube,
                    parent,
                    center + direction * 2.62f + Vector3.up * 0.22f,
                    new Vector3(0.28f, 0.10f, 0.72f),
                    accent,
                    new Vector3(0f, angle, 0f)
                );
            }

            CreateRuntimeWorldPart(
                "EclipseRitualCore",
                PrimitiveType.Sphere,
                parent,
                center + Vector3.up * 0.42f,
                Vector3.one * 0.56f,
                Color.Lerp(accent, Color.white, 0.1f),
                Vector3.zero
            );
        }

        private void CreateEclipseBrazier(
            Transform parent,
            Vector3 position,
            Color stone,
            Color flame)
        {
            CreateRuntimeWorldPart(
                "EclipseBrazierBase",
                PrimitiveType.Cylinder,
                parent,
                position + Vector3.up * 0.15f,
                new Vector3(0.68f, 0.15f, 0.68f),
                stone,
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "EclipseBrazierBowl",
                PrimitiveType.Sphere,
                parent,
                position + Vector3.up * 0.56f,
                new Vector3(0.48f, 0.20f, 0.48f),
                Color.Lerp(stone, Color.black, 0.38f),
                Vector3.zero
            );
            CreateRuntimeWorldPart(
                "EclipseBrazierFlame",
                PrimitiveType.Capsule,
                parent,
                position + Vector3.up * 1.05f,
                new Vector3(0.20f, 0.46f, 0.20f),
                flame,
                new Vector3(0f, 0f, 15f)
            );
        }

        private void CreateSeasonalAccents(
            Transform parent,
            int season,
            Vector3[] anchors,
            Color mainColor,
            Color accent)
        {
            for (int index = 0; index < anchors.Length; index++)
            {
                switch (season)
                {
                    case 0:
                        CreateBloomCluster(parent, anchors[index], mainColor, accent);
                        break;
                    case 1:
                        CreateFireflySwarm(parent, anchors[index], accent, index);
                        break;
                    case 2:
                        CreateLeafPile(parent, anchors[index], mainColor, accent);
                        break;
                    default:
                        CreateFrostCluster(parent, anchors[index], mainColor, accent);
                        break;
                }
            }
        }

        private void CreateBloomCluster(
            Transform parent,
            Vector3 center,
            Color petals,
            Color accent)
        {
            CreateRuntimeWorldPart(
                "SeasonBloomStem",
                PrimitiveType.Cylinder,
                parent,
                center + Vector3.up * 0.24f,
                new Vector3(0.07f, 0.24f, 0.07f),
                new Color(0.12f, 0.42f, 0.16f),
                Vector3.zero
            );
            for (int petal = 0; petal < 4; petal++)
            {
                float angle = petal * 90f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward;
                CreateRuntimeWorldPart(
                    "SeasonBloom",
                    PrimitiveType.Sphere,
                    parent,
                    center + Vector3.up * 0.55f + direction * 0.22f,
                    new Vector3(0.27f, 0.10f, 0.20f),
                    petals,
                    new Vector3(0f, angle, 0f)
                );
            }
            CreateRuntimeWorldPart(
                "SeasonBloomCenter",
                PrimitiveType.Sphere,
                parent,
                center + Vector3.up * 0.56f,
                Vector3.one * 0.12f,
                accent,
                Vector3.zero
            );
        }

        private void CreateFireflySwarm(
            Transform parent,
            Vector3 center,
            Color glow,
            int seed)
        {
            for (int firefly = 0; firefly < 4; firefly++)
            {
                float angle = seed * 38f + firefly * 91f;
                float radius = 0.32f + firefly * 0.18f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward;
                CreateRuntimeWorldPart(
                    "SummerFirefly",
                    PrimitiveType.Sphere,
                    parent,
                    center + direction * radius +
                    Vector3.up * (0.42f + (firefly % 2) * 0.34f),
                    Vector3.one * 0.11f,
                    Color.Lerp(glow, Color.white, 0.3f),
                    Vector3.zero
                );
            }
        }

        private void CreateLeafPile(
            Transform parent,
            Vector3 center,
            Color leaves,
            Color accent)
        {
            for (int leaf = 0; leaf < 5; leaf++)
            {
                float angle = leaf * 72f + 18f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward;
                CreateRuntimeWorldPart(
                    "AutumnLeafPile",
                    PrimitiveType.Sphere,
                    parent,
                    center + direction * (0.18f + leaf * 0.09f) +
                    Vector3.up * (0.08f + leaf * 0.016f),
                    new Vector3(0.38f, 0.055f, 0.28f),
                    leaf % 2 == 0 ? leaves : accent,
                    new Vector3(0f, angle, 0f)
                );
            }
        }

        private void CreateFrostCluster(
            Transform parent,
            Vector3 center,
            Color frost,
            Color accent)
        {
            for (int shard = 0; shard < 3; shard++)
            {
                float angle = shard * 120f + 24f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward;
                float height = 0.58f + shard * 0.18f;
                CreateRuntimeWorldPart(
                    "WinterFrostCrystal",
                    PrimitiveType.Capsule,
                    parent,
                    center + direction * (0.28f + shard * 0.16f) +
                    Vector3.up * (height * 0.5f),
                    new Vector3(0.12f, height * 0.5f, 0.12f),
                    Color.Lerp(frost, accent, 0.38f),
                    new Vector3(8f, angle, 12f)
                );
            }
        }

        private static Color GetFoliageColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(1f, 0.45f, 0.70f),
                    1 => new Color(0.13f, 0.62f, 0.24f),
                    2 => new Color(0.78f, 0.17f, 0.045f),
                    _ => new Color(0.74f, 0.84f, 0.92f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.30f, 0.76f, 0.36f),
                    1 => new Color(0.035f, 0.38f, 0.14f),
                    2 => new Color(0.74f, 0.42f, 0.06f),
                    _ => new Color(0.22f, 0.48f, 0.46f)
                };
            }

            return season switch
            {
                0 => new Color(0.58f, 0.20f, 0.86f),
                1 => new Color(0.08f, 0.34f, 0.74f),
                2 => new Color(0.86f, 0.10f, 0.16f),
                _ => new Color(0.50f, 0.70f, 0.95f)
            };
        }

        private static Color GetPetalColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(1f, 0.66f, 0.82f),
                    1 => new Color(1f, 0.88f, 0.42f),
                    2 => new Color(0.98f, 0.43f, 0.08f),
                    _ => new Color(0.90f, 0.96f, 1f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.80f, 1f, 0.52f),
                    1 => new Color(0.42f, 0.88f, 0.46f),
                    2 => new Color(1f, 0.66f, 0.12f),
                    _ => new Color(0.72f, 0.90f, 0.92f)
                };
            }

            return season switch
            {
                0 => new Color(0.86f, 0.60f, 1f),
                1 => new Color(0.38f, 0.82f, 1f),
                2 => new Color(1f, 0.38f, 0.30f),
                _ => new Color(0.82f, 0.94f, 1f)
            };
        }

        private static Color GetWaterColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(0.08f, 0.50f, 0.68f),
                    1 => new Color(0.04f, 0.60f, 0.64f),
                    2 => new Color(0.08f, 0.30f, 0.52f),
                    _ => new Color(0.34f, 0.70f, 0.84f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.08f, 0.62f, 0.52f),
                    1 => new Color(0.02f, 0.42f, 0.34f),
                    2 => new Color(0.10f, 0.30f, 0.32f),
                    _ => new Color(0.28f, 0.56f, 0.64f)
                };
            }

            return season switch
            {
                0 => new Color(0.26f, 0.14f, 0.58f),
                1 => new Color(0.08f, 0.26f, 0.62f),
                2 => new Color(0.34f, 0.05f, 0.16f),
                _ => new Color(0.34f, 0.62f, 0.92f)
            };
        }

        private static Color GetLanternColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(1f, 0.58f, 0.76f),
                    1 => new Color(1f, 0.84f, 0.22f),
                    2 => new Color(1f, 0.34f, 0.08f),
                    _ => new Color(0.74f, 0.88f, 1f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.78f, 1f, 0.32f),
                    1 => new Color(0.34f, 1f, 0.56f),
                    2 => new Color(1f, 0.58f, 0.08f),
                    _ => new Color(0.58f, 0.90f, 1f)
                };
            }

            return season switch
            {
                0 => new Color(0.90f, 0.38f, 1f),
                1 => new Color(0.22f, 0.78f, 1f),
                2 => new Color(1f, 0.18f, 0.16f),
                _ => new Color(0.72f, 0.86f, 1f)
            };
        }

        private static Color GetSkyColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(0.42f, 0.62f, 0.82f),
                    1 => new Color(0.22f, 0.62f, 0.78f),
                    2 => new Color(0.55f, 0.28f, 0.20f),
                    _ => new Color(0.46f, 0.62f, 0.78f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.22f, 0.54f, 0.40f),
                    1 => new Color(0.04f, 0.30f, 0.22f),
                    2 => new Color(0.42f, 0.28f, 0.12f),
                    _ => new Color(0.24f, 0.42f, 0.48f)
                };
            }

            return season switch
            {
                0 => new Color(0.20f, 0.06f, 0.36f),
                1 => new Color(0.04f, 0.14f, 0.36f),
                2 => new Color(0.30f, 0.035f, 0.09f),
                _ => new Color(0.12f, 0.22f, 0.42f)
            };
        }

        private static Color GetGroundColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(0.16f, 0.44f, 0.20f),
                    1 => new Color(0.10f, 0.38f, 0.15f),
                    2 => new Color(0.34f, 0.22f, 0.09f),
                    _ => new Color(0.48f, 0.56f, 0.60f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.08f, 0.34f, 0.16f),
                    1 => new Color(0.025f, 0.24f, 0.09f),
                    2 => new Color(0.25f, 0.18f, 0.055f),
                    _ => new Color(0.20f, 0.34f, 0.38f)
                };
            }

            return season switch
            {
                0 => new Color(0.15f, 0.06f, 0.24f),
                1 => new Color(0.035f, 0.10f, 0.25f),
                2 => new Color(0.24f, 0.035f, 0.055f),
                _ => new Color(0.20f, 0.30f, 0.42f)
            };
        }

        private static Color GetAccentColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(1f, 0.80f, 0.32f),
                    1 => new Color(1f, 0.94f, 0.42f),
                    2 => new Color(1f, 0.36f, 0.08f),
                    _ => new Color(0.78f, 0.92f, 1f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.72f, 1f, 0.34f),
                    1 => new Color(0.22f, 0.92f, 0.50f),
                    2 => new Color(1f, 0.62f, 0.10f),
                    _ => new Color(0.64f, 0.88f, 1f)
                };
            }

            return season switch
            {
                0 => new Color(0.90f, 0.36f, 1f),
                1 => new Color(0.18f, 0.86f, 1f),
                2 => new Color(1f, 0.22f, 0.12f),
                _ => new Color(0.80f, 0.92f, 1f)
            };
        }

        private static Color GetStoneColor(int scenario, int season)
        {
            Color baseStone = scenario switch
            {
                0 => new Color(0.31f, 0.34f, 0.31f),
                1 => new Color(0.22f, 0.32f, 0.26f),
                _ => new Color(0.20f, 0.15f, 0.31f)
            };
            return season == 3
                ? Color.Lerp(baseStone, new Color(0.74f, 0.84f, 0.92f), 0.42f)
                : baseStone;
        }

        private static void ApplyAtmosphere(
            int scenario,
            int season,
            Color ground)
        {
            Color sky = GetSkyColor(scenario, season);
            Camera gameCamera = Camera.main;
            if (gameCamera != null)
            {
                gameCamera.backgroundColor = sky;
            }

            GameObject horizon = GameObject.Find("Runtime Horizon Ground");
            Renderer horizonRenderer = horizon != null
                ? horizon.GetComponent<Renderer>()
                : null;
            if (horizonRenderer != null)
            {
                horizonRenderer.material.color = Color.Lerp(ground, sky, 0.12f);
            }

            RenderSettings.fog = scenario == 2 || season == 3;
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = Color.Lerp(sky, ground, 0.45f);
                RenderSettings.fogDensity = scenario == 2 ? 0.009f : 0.0035f;
            }
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            foreach (string term in terms)
            {
                if (value.Contains(term))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<Transform> GetJoints(Transform path)
        {
            List<Transform> joints = new();
            foreach (Transform child in path)
            {
                if (child.name.StartsWith("Joint_"))
                {
                    joints.Add(child);
                }
            }
            joints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return joints;
        }

        private static void UpdateRibbon(
            Transform path,
            string ribbonName,
            Vector3[] points,
            float width,
            float height,
            bool updateCollider)
        {
            Transform ribbon = path.Find(ribbonName);
            if (ribbon == null)
            {
                return;
            }

            MeshFilter filter = ribbon.GetComponent<MeshFilter>();
            if (filter == null)
            {
                return;
            }

            Mesh mesh = filter.mesh;
            Vector3[] vertices = new Vector3[points.Length * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(points.Length - 1) * 6];
            float travelled = 0f;

            for (int index = 0; index < points.Length; index++)
            {
                Vector3 previous = points[Mathf.Max(index - 1, 0)];
                Vector3 next = points[Mathf.Min(index + 1, points.Length - 1)];
                Vector3 tangent = next - previous;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.0001f)
                {
                    tangent = Vector3.forward;
                }
                tangent.Normalize();

                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                Vector3 center = new Vector3(points[index].x, height, points[index].z);
                vertices[index * 2] = center - side * width * 0.5f;
                vertices[index * 2 + 1] = center + side * width * 0.5f;

                if (index > 0)
                {
                    travelled += Vector3.Distance(points[index - 1], points[index]);
                }
                uv[index * 2] = new Vector2(0f, travelled * 0.32f);
                uv[index * 2 + 1] = new Vector2(1f, travelled * 0.32f);

                if (index >= points.Length - 1)
                {
                    continue;
                }

                int triangle = index * 6;
                int vertex = index * 2;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (updateCollider)
            {
                MeshCollider collider = ribbon.GetComponent<MeshCollider>();
                if (collider != null)
                {
                    collider.sharedMesh = null;
                    collider.sharedMesh = mesh;
                }
            }
        }
    }
}
