using UnityEngine;

namespace CatsVsDemons.Player
{
    public sealed class KinHealthBar : MonoBehaviour
    {
        [SerializeField] private KinHealth kinHealth;
        [SerializeField] private Transform fill;
        [SerializeField] private Renderer fillRenderer;
        [SerializeField] private float fullWidth = 1.8f;

        private Camera mainCamera;
        private Material fillMaterial;

        public void Initialize(
            KinHealth health,
            Transform fillTransform,
            Renderer renderer,
            float width)
        {
            kinHealth = health;
            fill = fillTransform;
            fillRenderer = renderer;
            fullWidth = width;
        }

        private void Awake()
        {
            mainCamera = Camera.main;

            if (fillRenderer != null)
            {
                fillMaterial = fillRenderer.material;
            }
        }

        private void OnEnable()
        {
            if (kinHealth != null)
            {
                kinHealth.HealthChanged += UpdateBar;
            }
        }

        private void Start()
        {
            if (fillRenderer != null && fillMaterial == null)
            {
                fillMaterial = fillRenderer.material;
            }

            if (kinHealth != null)
            {
                UpdateBar(
                    kinHealth.CurrentHealth,
                    kinHealth.MaxHealth
                );
            }
        }

        private void OnDisable()
        {
            if (kinHealth != null)
            {
                kinHealth.HealthChanged -= UpdateBar;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }

        private void UpdateBar(int current, int maximum)
        {
            if (fill == null || maximum <= 0)
            {
                return;
            }

            float normalized = Mathf.Clamp01(
                (float)current / maximum
            );

            Vector3 scale = fill.localScale;
            scale.x = fullWidth * normalized;
            fill.localScale = scale;

            Vector3 position = fill.localPosition;
            position.x = -(fullWidth - scale.x) * 0.5f;
            fill.localPosition = position;

            if (fillMaterial == null && fillRenderer != null)
            {
                fillMaterial = fillRenderer.material;
            }

            if (fillMaterial != null)
            {
                fillMaterial.color = GetHealthColor(normalized);
            }
        }

        private static Color GetHealthColor(float normalized)
        {
            if (normalized > 0.6f)
            {
                return new Color(0.1f, 0.9f, 0.18f);
            }

            if (normalized > 0.3f)
            {
                return new Color(1f, 0.82f, 0.05f);
            }

            return new Color(0.95f, 0.08f, 0.06f);
        }
    }
}
