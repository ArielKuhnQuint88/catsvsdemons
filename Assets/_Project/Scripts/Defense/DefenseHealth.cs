using System;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class DefenseHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 80;
        [SerializeField] private float barHeight = 3f;

        private BuildSpot owner;
        private Transform barRoot;
        private Transform barFill;
        private Camera mainCamera;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDestroyed => CurrentHealth <= 0;
        public event Action<int, int> HealthChanged;

        public void Initialize(BuildSpot buildSpot, int health, float height)
        {
            owner = buildSpot;
            maxHealth = Mathf.Max(1, health);
            barHeight = height;
            CurrentHealth = maxHealth;
            CreateHealthBar();
            UpdateHealthBar();
        }

        private void Awake()
        {
            CurrentHealth = maxHealth;
            mainCamera = Camera.main;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDestroyed)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            UpdateHealthBar();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (!IsDestroyed)
            {
                return;
            }

            if (owner != null)
            {
                owner.NotifyDefenseDestroyed(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            if (barRoot == null)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                barRoot.rotation = mainCamera.transform.rotation;
            }
        }

        private void CreateHealthBar()
        {
            if (barRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("DefenseHealthBar");
            root.transform.SetParent(transform);
            root.transform.localPosition = new Vector3(0f, barHeight, 0f);
            barRoot = root.transform;

            CreateBarPart(
                "Background",
                root.transform,
                new Vector3(0f, 0f, 0.01f),
                new Vector3(1.7f, 0.18f, 0.04f),
                new Color(0.04f, 0.04f, 0.05f)
            );
            barFill = CreateBarPart(
                "Fill",
                root.transform,
                new Vector3(0f, 0f, -0.02f),
                new Vector3(1.55f, 0.12f, 0.035f),
                new Color(0.18f, 0.85f, 0.28f)
            ).transform;
        }

        private void UpdateHealthBar()
        {
            if (barFill == null || maxHealth <= 0)
            {
                return;
            }

            float normalized = Mathf.Clamp01((float)CurrentHealth / maxHealth);
            Vector3 scale = barFill.localScale;
            scale.x = 1.55f * normalized;
            barFill.localScale = scale;

            Vector3 position = barFill.localPosition;
            position.x = -(1.55f - scale.x) * 0.5f;
            barFill.localPosition = position;

            Renderer renderer = barFill.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color =
                    normalized > 0.6f
                        ? new Color(0.18f, 0.85f, 0.28f)
                        : normalized > 0.3f
                            ? new Color(1f, 0.7f, 0.08f)
                            : new Color(0.92f, 0.12f, 0.08f);
            }
        }

        private static GameObject CreateBarPart(
            string partName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            Material material = new Material(shader);
            material.color = color;
            part.GetComponent<Renderer>().material = material;
            return part;
        }
    }
}
