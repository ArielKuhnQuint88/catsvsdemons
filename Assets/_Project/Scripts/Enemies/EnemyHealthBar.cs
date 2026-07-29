using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private Transform fill;
        [SerializeField] private float fullWidth = 1.4f;

        private Camera mainCamera;

        public void Initialize(
            EnemyHealth health,
            Transform fillTransform,
            float width)
        {
            enemyHealth = health;
            fill = fillTransform;
            fullWidth = width;
        }

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.HealthChanged += UpdateBar;
            }
        }

        private void Start()
        {
            if (enemyHealth != null)
            {
                UpdateBar(
                    enemyHealth.CurrentHealth,
                    enemyHealth.MaxHealth
                );
            }
        }

        private void OnDisable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.HealthChanged -= UpdateBar;
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
        }
    }
}
