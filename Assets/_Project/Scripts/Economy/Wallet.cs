using System;
using UnityEngine;

namespace CatsVsDemons.Economy
{
    public sealed class Wallet : MonoBehaviour
    {
        [SerializeField] private int startingCoins;

        public int Coins { get; private set; }
        public event Action<int> CoinsChanged;

        private void Awake()
        {
            Coins = Mathf.Max(0, startingCoins);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            CoinsChanged?.Invoke(Coins);
            Debug.Log($"Coins: {Coins}");
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }

        public void SetCoins(int amount)
        {
            Coins = Mathf.Max(0, amount);
            CoinsChanged?.Invoke(Coins);
        }
    }
}
