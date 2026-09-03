using System;
using UnityEngine;

namespace CatsVsDemons.Player
{
    public sealed class KinEnergy : MonoBehaviour
    {
        [SerializeField] private float maximum = 100f;
        [SerializeField] private float startingEnergy = 35f;
        private float baseMaximum;
        public float Current { get; private set; }
        public float Maximum => maximum;
        public float Normalized => maximum > 0f ? Current / maximum : 0f;
        public bool IsFull => Current >= maximum - 0.01f;
        public event Action<float, float> Changed;

        private void Awake()
        {
            baseMaximum = Mathf.Max(1f, maximum);
            maximum = baseMaximum;
            Current = Mathf.Clamp(startingEnergy, 0f, maximum);
        }
        private void Start() => Changed?.Invoke(Current, maximum);

        public void Add(float amount)
        {
            if (amount <= 0f || IsFull) return;
            Current = Mathf.Min(maximum, Current + amount);
            Changed?.Invoke(Current, maximum);
        }

        public bool TryConsumeAll()
        {
            if (!IsFull) return false;
            Current = 0f;
            Changed?.Invoke(Current, maximum);
            return true;
        }

        public void SetShopMaximumBonus(float bonus)
        {
            float previousMaximum = maximum;
            maximum = baseMaximum + Mathf.Max(0f, bonus);
            Current = Mathf.Clamp(
                Current + maximum - previousMaximum,
                0f,
                maximum
            );
            Changed?.Invoke(Current, maximum);
        }
    }
}
