using System;
using CatsVsDemons.Economy;
using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private int coinReward = 5;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0;

        public event Action<int, int> HealthChanged;

        private Wallet wallet;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
        }

        private void Start()
        {
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void Configure(int health, int reward)
        {
            maxHealth = Mathf.Max(1, health);
            coinReward = Mathf.Max(0, reward);
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            Debug.Log($"{name} health: {CurrentHealth}/{maxHealth}");

            if (IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            if (wallet == null)
            {
                wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
            }

            if (wallet != null)
            {
                wallet.AddCoins(coinReward);
            }

            Destroy(gameObject);
        }
    }
}
