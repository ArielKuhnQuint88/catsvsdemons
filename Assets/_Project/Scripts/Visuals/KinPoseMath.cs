using UnityEngine;

namespace CatsVsDemons.Visuals
{
    internal static class KinPoseMath
    {
        public const float AttackDuration = .38f;

        // Preparation, strike and recovery all reach their endpoints with zero
        // velocity. Body, wrist and first-person hand share the same timing.
        public static float Swing(float progress) => Pulse(progress, .18f, .46f, 1f);
        public static float Anticipation(float progress) => Pulse(progress, 0f, .18f, .42f);
        private static float Pulse(float progress, float start, float peak, float end)
        {
            if (progress <= start || progress >= end) return 0f;
            float t = progress < peak ? (progress - start) / (peak - start) :
                (end - progress) / (end - peak);
            return t * t * (3f - 2f * t);
        }
        public static float AdvanceGait(float phase, float signedSpeed, float dt) =>
            phase + signedSpeed * dt * 1.8f;
    }
}
