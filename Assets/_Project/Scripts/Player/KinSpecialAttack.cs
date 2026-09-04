using System;
using System.Collections;
using CatsVsDemons.Enemies;
using CatsVsDemons.Feedback;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatsVsDemons.Player
{
    [RequireComponent(typeof(KinEnergy))]
    public sealed class KinSpecialAttack : MonoBehaviour
    {
        [SerializeField] private float radius = 6f;
        [SerializeField] private int damage = 35;
        private KinEnergy energy;
        private LineRenderer ring;
        private int baseDamage;
        public event Action Used;

        public int Damage => damage;

        private void Awake()
        {
            baseDamage = Mathf.Max(1, damage);
            energy = GetComponent<KinEnergy>();
        }

        public void SetShopDamageBonus(int bonus)
        {
            damage = baseDamage + Mathf.Max(0, bonus);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                TryUse();
        }

        public bool TryUse()
        {
            if (energy == null || !energy.TryConsumeAll()) return false;
            var targets = EnemyRegistry.GetInRange(transform.position, radius);
            for (int index = targets.Count - 1; index >= 0; index--)
                targets[index].TakeDamage(damage);
            GameFeedback.PlaySpecial();
            StartCoroutine(AnimateRing());
            Used?.Invoke();
            return true;
        }

        private IEnumerator AnimateRing()
        {
            EnsureRing();
            float elapsed = 0f;
            ring.enabled = true;
            while (elapsed < 0.42f)
            {
                elapsed += Time.deltaTime;
                float amount = Mathf.Clamp01(elapsed / 0.42f);
                UpdateRing(Mathf.Lerp(0.4f, radius, amount));
                Color color = new(1f, 0.72f, 0.12f, 1f - amount);
                ring.startColor = ring.endColor = color;
                yield return null;
            }
            ring.enabled = false;
        }

        private void EnsureRing()
        {
            if (ring != null) return;
            GameObject visual = new("Samurai Energy Wave");
            visual.transform.SetParent(transform, false);
            ring = visual.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = false;
            ring.positionCount = 64;
            ring.startWidth = ring.endWidth = 0.22f;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");
            ring.material = new Material(shader);
        }

        private void UpdateRing(float currentRadius)
        {
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(index, new Vector3(
                    Mathf.Cos(angle) * currentRadius, 0.25f,
                    Mathf.Sin(angle) * currentRadius));
            }
        }
    }
}
