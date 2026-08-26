using UnityEngine;

namespace CatsVsDemons.House
{
    public sealed class HouseHealthBar : MonoBehaviour
    {
        [SerializeField] private HouseHealth houseHealth;
        [SerializeField] private Transform fill;
        [SerializeField] private float fullWidth = 4f;
        [SerializeField] private float minimumWorldHeight = 8.35f;

        private Camera mainCamera;

        private void OnEnable()
        {
            if (houseHealth != null)
            {
                houseHealth.HealthChanged += UpdateBar;
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;

            if (houseHealth != null)
            {
                UpdateBar(
                    houseHealth.CurrentHealth,
                    houseHealth.MaxHealth
                );
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

            Vector3 position = transform.position;
            if (position.y < minimumWorldHeight)
            {
                position.y = minimumWorldHeight;
                transform.position = position;
            }
        }

        private void OnDisable()
        {
            if (houseHealth != null)
            {
                houseHealth.HealthChanged -= UpdateBar;
            }
        }

        public void Initialize(
            HouseHealth health,
            Transform fillTransform,
            float width)
        {
            houseHealth = health;
            fill = fillTransform;
            fullWidth = width;
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
