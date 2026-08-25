using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Defense
{
    public sealed class DefenseHealth : MonoBehaviour
    {
        private const float FullWidth = 1.75f;

        [SerializeField] private int maxHealth = 80;
        [SerializeField] private float barHeight = 3f;

        private BuildSpot owner;
        private Transform barRoot;
        private Transform barFill;
        private Renderer barFillRenderer;
        private Camera mainCamera;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDestroyed => CurrentHealth <= 0;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            mainCamera = Camera.main;
        }

        public void Initialize(BuildSpot buildSpot, int health, float height)
        {
            owner = buildSpot;
            maxHealth = Mathf.Max(1, health);
            barHeight = height;
            CurrentHealth = maxHealth;

            EnsureHealthBar();
            UpdateHealthBar();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDestroyed)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            EnsureHealthBar();
            UpdateHealthBar();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (!IsDestroyed)
                return;

            if (owner != null)
                owner.NotifyDefenseDestroyed(gameObject);
            else
                Destroy(gameObject);
        }

        private void LateUpdate()
        {
            EnsureHealthBar();

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null && barRoot != null)
            {
                barRoot.rotation = Quaternion.LookRotation(
                    mainCamera.transform.forward,
                    mainCamera.transform.up
                );
            }
        }

        private void EnsureHealthBar()
        {
            if (barRoot != null && barFill != null)
                return;

            Transform oldBar = transform.Find("DefenseHealthBar");
            if (oldBar != null)
                Destroy(oldBar.gameObject);

            GameObject root = new("DefenseHealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, barHeight, 0f);
            root.transform.localScale = Vector3.one;
            barRoot = root.transform;

            CreateBarPart(
                "Background",
                barRoot,
                new Vector3(0f, 0f, 0.025f),
                new Vector3(FullWidth + 0.16f, 0.24f, 0.07f),
                new Color(0.025f, 0.025f, 0.03f)
            );

            GameObject fillObject = CreateBarPart(
                "Fill",
                barRoot,
                new Vector3(0f, 0f, -0.025f),
                new Vector3(FullWidth, 0.15f, 0.08f),
                new Color(0.1f, 0.9f, 0.18f)
            );
            barFill = fillObject.transform;
            barFillRenderer = fillObject.GetComponent<Renderer>();
        }

        private void UpdateHealthBar()
        {
            if (barFill == null || maxHealth <= 0)
                return;

            float normalized = Mathf.Clamp01((float)CurrentHealth / maxHealth);
            Vector3 scale = barFill.localScale;
            scale.x = FullWidth * normalized;
            barFill.localScale = scale;

            Vector3 position = barFill.localPosition;
            position.x = -(FullWidth - scale.x) * 0.5f;
            barFill.localPosition = position;

            if (barFillRenderer == null)
                barFillRenderer = barFill.GetComponent<Renderer>();

            if (barFillRenderer != null)
            {
                barFillRenderer.material.color = normalized > 0.6f
                    ? new Color(0.1f, 0.9f, 0.18f)
                    : normalized > 0.3f
                        ? new Color(1f, 0.75f, 0.05f)
                        : new Color(0.95f, 0.08f, 0.06f);
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
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = scale;

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
                Destroy(partCollider);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 1000;

            Material material = new(shader);
            material.color = color;
            material.renderQueue = 4000;
            renderer.material = material;
            return part;
        }
    }
}
