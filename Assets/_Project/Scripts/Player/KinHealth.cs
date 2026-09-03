using System;
using UnityEngine;

namespace CatsVsDemons.Player
{
    public sealed class KinHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        private int baseMaxHealth;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDown => CurrentHealth <= 0;

        public event Action<int, int> HealthChanged;
        public event Action Downed;

        private void Awake()
        {
            baseMaxHealth = Mathf.Max(1, maxHealth);
            maxHealth = baseMaxHealth;
            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDown)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (IsDown)
            {
                Debug.Log("Kin caiu!");
                Downed?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDown)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void SetShopMaximumBonus(int bonus)
        {
            int previousMaximum = maxHealth;
            bool wasDown = IsDown;
            maxHealth = baseMaxHealth + Mathf.Max(0, bonus);
            if (!wasDown)
            {
                CurrentHealth = Mathf.Clamp(
                    CurrentHealth + maxHealth - previousMaximum,
                    1,
                    maxHealth
                );
            }
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
