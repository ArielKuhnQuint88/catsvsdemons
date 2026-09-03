using System.Collections.Generic;
using CatsVsDemons.CameraSystem;
using UnityEngine;

namespace CatsVsDemons.Waves
{
    public sealed class PhaseEnvironmentController : MonoBehaviour
    {
        private const float SurfaceWidth = 2.45f;
        private const float BorderWidth = 2.85f;
        private const float SurfaceHeight = 0.12f;
        private const float BorderHeight = 0.025f;
        private const float ViewportMargin = 0.08f;
        private const float EntrancePadding = 3f;
        private const float MaximumEntranceExtension = 48f;

        private readonly Dictionary<Transform, Vector3[]> basePaths = new();
        private readonly Dictionary<Transform, Vector3> baseBuildSpots = new();
        private readonly Dictionary<Renderer, Color> baseColors = new();

        private EnemyWaveSpawner spawner;
        private Transform pathsRoot;
        private Transform buildSpotsRoot;
        private CameraModeController cameraController;
        private bool captured;
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
            ApplyPhase(Mathf.Max(1, spawner.CurrentPhase), spawner.TotalPhases);
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
            ApplyGardenTheme(phase);
            pendingPathRefreshPhase = phase;

            string title = phase == 1
                ? "Jardim das Cerejeiras"
                : phase == 2
                    ? "Jardim do Outono"
                    : "Jardim do Eclipse";

            Debug.Log($"Phase {phase}: {title} loaded.", this);
        }

        private void ApplyPathLayout(int phase)
        {
            float minimumExtension =
                phase == 1 ? 12f : phase == 2 ? 16f : 20f;
            float waveAmplitude =
                phase == 1 ? 0f : phase == 2 ? 0.45f : 0.75f;
            float waveCycles =
                phase == 1 ? 0f : phase == 2 ? 2.0f : 3.0f;
            int pathIndex = 0;

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

                    float entranceWeight =
                        1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.36f));
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
                    float centerEnvelope = Mathf.Sin(t * Mathf.PI);
                    float houseProtection =
                        1f - Mathf.SmoothStep(0.55f, 0.82f, t);
                    float wave = Mathf.Sin(
                        (t * waveCycles * Mathf.PI * 2f) + pathIndex * 0.83f
                    );

                    Vector3 position =
                        baseline[index] +
                        extension +
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
            float radialScale = phase == 1 ? 1f : phase == 2 ? 1.06f : 1.12f;
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
            foreach (KeyValuePair<Renderer, Color> entry in baseColors)
            {
                Renderer renderer = entry.Key;
                if (renderer == null)
                {
                    continue;
                }

                Color color = entry.Value;
                string objectName = renderer.gameObject.name;

                if (phase == 2)
                {
                    if (ContainsAny(objectName, "Blossoms", "MapleCrown"))
                        color = new Color(0.92f, 0.25f, 0.035f);
                    else if (ContainsAny(objectName, "Petal"))
                        color = new Color(1f, 0.58f, 0.10f);
                }
                else if (phase >= 3)
                {
                    if (ContainsAny(objectName, "Blossoms", "MapleCrown"))
                        color = new Color(0.64f, 0.08f, 0.48f);
                    else if (ContainsAny(objectName, "WaterHighlight"))
                        color = new Color(0.08f, 0.48f, 0.62f);
                    else if (ContainsAny(objectName, "Petal"))
                        color = new Color(0.68f, 0.30f, 1f);
                    else if (ContainsAny(objectName, "LanternLight"))
                        color = new Color(0.84f, 0.30f, 1f);
                }

                renderer.material.color = color;
            }

            RenderSettings.fog = false;
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
