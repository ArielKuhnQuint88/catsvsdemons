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
        private const float PebbleSpacing = 1.45f;
        private const float PebbleEdgeOffset = BorderWidth * 0.5f + 0.22f;

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
        private bool captured;
        private bool gardenLandmarksRestored;
        private int pendingPathRefreshPhase;

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
            ClearChildren(pathBorderRoot);

            int pathIndex = 0;
            foreach (KeyValuePair<Transform, Vector3[]> entry in laidOutPaths)
            {
                if (entry.Key != null && entry.Value.Length > 1)
                {
                    CreatePebbleBorders(pathBorderRoot, entry.Key, entry.Value, pathIndex);
                }
                pathIndex++;
            }
        }

        private void CreatePebbleBorders(
            Transform parent,
            Transform path,
            Vector3[] points,
            int pathIndex)
        {
            float pathLength = GetPathLength(path, points);
            if (pathLength < 0.5f)
            {
                return;
            }

            Color[] pebbleColors =
            {
                new Color(0.24f, 0.27f, 0.25f),
                new Color(0.36f, 0.35f, 0.29f),
                new Color(0.30f, 0.33f, 0.31f)
            };

            int sampleIndex = 0;
            for (
                float distance = 0.55f;
                distance < pathLength - 0.35f;
                distance += PebbleSpacing
            )
            {
                SamplePath(path, points, distance, out Vector3 center, out Vector3 tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                if (side.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                for (int edge = -1; edge <= 1; edge += 2)
                {
                    float sizeJitter = Hash01(pathIndex, sampleIndex, edge + 5);
                    float positionJitter = Hash01(pathIndex, sampleIndex, edge + 13);
                    Vector3 position =
                        center +
                        side * edge * (PebbleEdgeOffset + (sizeJitter - 0.5f) * 0.16f) +
                        tangent * (positionJitter - 0.5f) * 0.22f;
                    position.y = center.y + 0.15f + sizeJitter * 0.045f;

                    Vector3 scale = new Vector3(
                        0.38f + sizeJitter * 0.18f,
                        0.12f + sizeJitter * 0.06f,
                        0.30f + positionJitter * 0.16f
                    );
                    float yaw =
                        Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg +
                        (positionJitter - 0.5f) * 26f;

                    CreateRuntimeWorldPart(
                        "PathPebble",
                        PrimitiveType.Sphere,
                        parent,
                        position,
                        scale,
                        pebbleColors[(sampleIndex + (edge > 0 ? 1 : 0)) % pebbleColors.Length],
                        new Vector3(0f, yaw, 0f)
                    );
                }

                sampleIndex++;
            }
        }

        private static float GetPathLength(Transform path, Vector3[] points)
        {
            float length = 0f;
            for (int index = 0; index < points.Length - 1; index++)
            {
                length += Vector3.Distance(
                    path.TransformPoint(points[index]),
                    path.TransformPoint(points[index + 1])
                );
            }
            return length;
        }

        private static void SamplePath(
            Transform path,
            Vector3[] points,
            float distance,
            out Vector3 position,
            out Vector3 tangent)
        {
            float remaining = distance;
            for (int index = 0; index < points.Length - 1; index++)
            {
                Vector3 start = path.TransformPoint(points[index]);
                Vector3 end = path.TransformPoint(points[index + 1]);
                Vector3 segment = end - start;
                float segmentLength = segment.magnitude;
                if (segmentLength < 0.0001f)
                {
                    continue;
                }

                if (remaining <= segmentLength)
                {
                    tangent = segment / segmentLength;
                    tangent.y = 0f;
                    tangent.Normalize();
                    position = Vector3.Lerp(start, end, remaining / segmentLength);
                    return;
                }

                remaining -= segmentLength;
            }

            position = path.TransformPoint(points[points.Length - 1]);
            tangent = path.TransformPoint(points[points.Length - 1]) -
                path.TransformPoint(points[points.Length - 2]);
            tangent.y = 0f;
            tangent = tangent.sqrMagnitude < 0.0001f
                ? Vector3.forward
                : tangent.normalized;
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

        private static float Hash01(int first, int second, int third)
        {
            float value = Mathf.Sin(
                first * 12.9898f + second * 78.233f + third * 37.719f
            ) * 43758.5453f;
            return value - Mathf.Floor(value);
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
            ApplyAtmosphere(scenario, season);
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

        private static Color GetFoliageColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season switch
                {
                    0 => new Color(1f, 0.42f, 0.66f),
                    1 => new Color(0.20f, 0.65f, 0.28f),
                    2 => new Color(0.92f, 0.25f, 0.035f),
                    _ => new Color(0.84f, 0.89f, 0.94f)
                };
            }

            if (scenario == 1)
            {
                return season switch
                {
                    0 => new Color(0.20f, 0.72f, 0.40f),
                    1 => new Color(0.08f, 0.48f, 0.18f),
                    2 => new Color(0.72f, 0.48f, 0.08f),
                    _ => new Color(0.36f, 0.68f, 0.62f)
                };
            }

            return season switch
            {
                0 => new Color(0.62f, 0.20f, 0.86f),
                1 => new Color(0.16f, 0.24f, 0.58f),
                2 => new Color(0.88f, 0.16f, 0.35f),
                _ => new Color(0.58f, 0.70f, 0.96f)
            };
        }

        private static Color GetPetalColor(int scenario, int season)
        {
            Color foliage = GetFoliageColor(scenario, season);
            float lightness = scenario == 2 ? 0.36f : 0.52f;
            return Color.Lerp(foliage, Color.white, lightness);
        }

        private static Color GetWaterColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season == 3
                    ? new Color(0.30f, 0.62f, 0.76f)
                    : new Color(0.06f, 0.46f, 0.62f);
            }

            if (scenario == 1)
            {
                return season == 2
                    ? new Color(0.08f, 0.38f, 0.42f)
                    : new Color(0.04f, 0.56f, 0.52f);
            }

            return season == 3
                ? new Color(0.36f, 0.58f, 0.86f)
                : new Color(0.10f, 0.16f, 0.45f);
        }

        private static Color GetLanternColor(int scenario, int season)
        {
            if (scenario == 2)
            {
                return season == 3
                    ? new Color(0.68f, 0.78f, 1f)
                    : new Color(0.82f, 0.30f, 1f);
            }

            return season == 2
                ? new Color(1f, 0.48f, 0.10f)
                : new Color(1f, 0.72f, 0.18f);
        }

        private static Color GetSkyColor(int scenario, int season)
        {
            if (scenario == 0)
            {
                return season == 3
                    ? new Color(0.38f, 0.55f, 0.72f)
                    : new Color(0.28f, 0.48f, 0.68f);
            }

            if (scenario == 1)
            {
                return season == 2
                    ? new Color(0.34f, 0.31f, 0.22f)
                    : new Color(0.12f, 0.38f, 0.34f);
            }

            return season == 3
                ? new Color(0.18f, 0.24f, 0.43f)
                : new Color(0.10f, 0.06f, 0.24f);
        }

        private static void ApplyAtmosphere(int scenario, int season)
        {
            Camera gameCamera = Camera.main;
            if (gameCamera != null)
            {
                gameCamera.backgroundColor = GetSkyColor(scenario, season);
            }

            RenderSettings.fog = scenario == 2 && season >= 2;
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = GetSkyColor(scenario, season);
                RenderSettings.fogDensity = 0.0075f;
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
