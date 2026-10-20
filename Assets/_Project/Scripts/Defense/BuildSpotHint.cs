using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Defense
{
    /// <summary>A world-space ring makes the second construction step visible without intercepting clicks.</summary>
    public sealed class BuildSpotHint : MonoBehaviour
    {
        private static Material ringMaterial;
        private static BuildSpot hovered;
        private readonly Vector3[] points = new Vector3[40];
        private BuildSpot spot;
        private Renderer platform;
        private LineRenderer ring;
        private float buildPulse;
        private float errorPulse;

        public static void SetHovered(BuildSpot value) => hovered = value;
        public void PlayBuildPulse() => buildPulse = 0.8f;
        public void PlayErrorPulse() => errorPulse = 0.6f;

        private void Awake()
        {
            spot = GetComponent<BuildSpot>();
            platform = GetComponent<Renderer>();
        }

        private void EnsureRing()
        {
            if (ring != null) return;
            if (ringMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) return;
                ringMaterial = new Material(shader) { name = "Build base guidance", hideFlags = HideFlags.HideAndDontSave };
                ringMaterial.SetColor("_BaseColor", Color.white);
                ringMaterial.SetFloat("_Surface", 1f);
                ringMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                ringMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                ringMaterial.SetFloat("_ZWrite", 0f);
                ringMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                ringMaterial.renderQueue = 3000;
            }
            GameObject visual = new("Free Base Halo");
            visual.transform.SetParent(transform, false);
            ring = visual.AddComponent<LineRenderer>();
            ring.useWorldSpace = true; ring.loop = true;
            ring.positionCount = points.Length;
            ring.sharedMaterial = ringMaterial;
            ring.widthMultiplier = 0.055f;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) { if (ring != null) ring.enabled = false; return; }
            bool choosing = TowerBuildSelection.AwaitingPlacement && !spot.IsOccupied;
            bool flash = buildPulse > 0f || errorPulse > 0f;
            if (!choosing && !flash) { if (ring != null) ring.enabled = false; return; }
            EnsureRing();
            if (ring == null) return;
            ring.enabled = true;
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4f) * 0.5f;
            float radius = platform != null ? Mathf.Max(platform.bounds.extents.x, platform.bounds.extents.z) + 0.14f : 0.85f;
            Vector3 center = transform.position;
            center.y = platform != null ? platform.bounds.max.y + 0.05f : center.y + 0.2f;
            float expansion = buildPulse > 0f ? (0.8f - buildPulse) * 1.1f : 0f;
            radius += expansion;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / points.Length;
                points[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            ring.SetPositions(points);
            Color color = errorPulse > 0f ? new Color(0.9f, 0.27f, 0.17f)
                : buildPulse > 0f ? new Color(0.56f, 0.95f, 0.7f)
                : hovered == spot ? new Color(1f, 0.94f, 0.62f) : new Color(0.89f, 0.74f, 0.4f);
            color.a = buildPulse > 0f ? buildPulse / 0.8f : 0.55f + pulse * 0.4f;
            ring.startColor = ring.endColor = color;
            ring.widthMultiplier = hovered == spot ? 0.085f : 0.055f;
            buildPulse = Mathf.Max(0f, buildPulse - Time.unscaledDeltaTime);
            errorPulse = Mathf.Max(0f, errorPulse - Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (hovered == spot) hovered = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
            ringMaterial = null; hovered = null;
        }
    }
}
