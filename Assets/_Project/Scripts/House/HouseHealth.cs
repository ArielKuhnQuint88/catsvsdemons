using System;
using UnityEngine;

namespace CatsVsDemons.House
{
    public sealed class HouseHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDestroyed => CurrentHealth <= 0;

        public event Action<int, int> HealthChanged;
        public event Action Destroyed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDestroyed)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            Debug.Log($"House took {amount} damage. Health: {CurrentHealth}/{maxHealth}");

            if (IsDestroyed)
            {
                Debug.Log("Game Over: the house was destroyed.");
                Destroyed?.Invoke();
            }
        }
    }
}
