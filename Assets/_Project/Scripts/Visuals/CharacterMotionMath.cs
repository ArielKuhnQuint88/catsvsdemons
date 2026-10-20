using UnityEngine;

namespace CatsVsDemons.Visuals
{
    internal enum CharacterMotionProfile { Kin, Sono, Poerix, Flamurk }

    internal readonly struct CharacterBodyPose
    {
        public readonly Vector3 Offset;
        public readonly Vector3 Euler;
        public CharacterBodyPose(Vector3 offset, Vector3 euler)
        { Offset = offset; Euler = euler; }
    }

    internal static class CharacterMotionMath
    {
        public static CharacterMotionProfile ProfileFor(string modelPath)
        {
            switch (modelPath)
            {
                case "Models/DemonSono": return CharacterMotionProfile.Sono;
                case "Models/DemonPoerix": return CharacterMotionProfile.Poerix;
                case "Models/DemonFlamurk": return CharacterMotionProfile.Flamurk;
                default: return CharacterMotionProfile.Kin;
            }
        }

        public static bool UsesArticulatedRig(string modelPath) => modelPath == "Models/Kin";

        public static CharacterBodyPose Evaluate(CharacterMotionProfile profile, float phase,
            float idlePhase, float movement, bool attacking, float progress)
        {
            float blend = Mathf.Clamp01(movement);
            float step = Mathf.Sin(phase);
            // Two smooth footfalls per cycle, without the cusp of Abs(Sin()).
            float lift = (1f - Mathf.Cos(phase * 2f)) * .5f * blend;
            float breathe = Mathf.Sin(idlePhase);
            Vector3 offset;
            Vector3 euler;
            switch (profile)
            {
                case CharacterMotionProfile.Sono:
                    // The sleep demon glides gently; its horns and face stay rigid.
                    offset = new Vector3(step * .012f * blend,
                        .035f + breathe * .022f + lift * .018f, 0f);
                    euler = new Vector3(-2f * blend, breathe * 1.1f,
                        step * 2f * blend + Mathf.Sin(idlePhase * .7f) * .8f);
                    break;
                case CharacterMotionProfile.Poerix:
                    offset = new Vector3(step * .016f * blend,
                        lift * .035f + (breathe + 1f) * .002f, 0f);
                    euler = new Vector3(-3.5f * blend, step * 1.2f * blend,
                        step * 3.2f * blend);
                    break;
                case CharacterMotionProfile.Flamurk:
                    offset = new Vector3(step * .012f * blend,
                        lift * .055f + (breathe + 1f) * .003f, 0f);
                    euler = new Vector3(-4f * blend + breathe * .7f,
                        step * 1.8f * blend, step * 2.6f * blend);
                    break;
                default:
                    offset = Vector3.up * (lift * .028f);
                    euler = new Vector3(-2.2f * blend, 0f, step * 1.1f * blend);
                    break;
            }

            if (attacking)
            {
                float strike = KinPoseMath.Swing(progress);
                float prepare = KinPoseMath.Anticipation(progress);
                float reach = profile == CharacterMotionProfile.Kin ? .075f : .13f;
                offset += new Vector3(0f, strike * .025f,
                    strike * reach - prepare * .025f);
                euler += new Vector3(prepare * 4f - strike * 8f,
                    prepare * 3f - strike * 5f, -strike * 1.5f);
            }
            return new CharacterBodyPose(offset, euler);
        }
    }
}
