using UnityEngine;

namespace CatsVsDemons.House
{
    public sealed class HouseHealthBar : MonoBehaviour
    {
        [SerializeField] private HouseHealth houseHealth;
        [SerializeField] private Transform fill;
        [SerializeField] private float fullWidth = 4f;

        private void OnEnable()
        {
            if (houseHealth != null)
            {
                houseHealth.HealthChanged += UpdateBar;
            }
        }

        private void Start()
        {
            if (houseHealth != null)
            {
                UpdateBar(
                    houseHealth.CurrentHealth,
                    houseHealth.MaxHealth
                );
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
