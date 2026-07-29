using CatsVsDemons.Economy;
using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private int coinReward = 5;

        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private Wallet wallet;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            wallet = Object.FindFirstObjectByType<Wallet>();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
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
                wallet = Object.FindFirstObjectByType<Wallet>();
            }

            if (wallet != null)
            {
                wallet.AddCoins(coinReward);
            }

            Destroy(gameObject);
        }
    }
}
