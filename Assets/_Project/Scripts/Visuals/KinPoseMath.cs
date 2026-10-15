using UnityEngine;

namespace CatsVsDemons.Visuals
{
    internal static class KinPoseMath
    {
        public const float AttackDuration = .32f;
        public static float Swing(float progress) => Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
        public static float Recovery(float progress) => Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI * 2);
        public static float AdvanceGait(float phase, float signedSpeed, float dt) =>
            phase + signedSpeed * dt * 1.45f;
    }
}
