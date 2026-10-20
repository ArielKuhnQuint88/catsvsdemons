using CatsVsDemons.Player;
using CatsVsDemons.Visuals;
using UnityEngine;

static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static void Near(Vector3 actual, Vector3 expected, string message) =>
        Check((actual - expected).magnitude < .001f, message);

    static void Main()
    {
        Vector2[] commands = [new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
            new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)];
        foreach (Vector2 input in commands)
        {
            Check(KinMotionMath.TurnWithMovement(input, false), "Every isometric direction must turn Kin.");
            Check(!KinMotionMath.TurnWithMovement(input, true), "First-person movement must preserve look direction.");
            foreach (float yaw in new[] { 0f, 45f, 90f, 180f, 270f })
            {
                float angle = yaw * MathF.PI / 180f;
                Vector3 viewForward = new(MathF.Sin(angle), -.7f, MathF.Cos(angle));
                Vector3 viewRight = new(MathF.Cos(angle), 0, -MathF.Sin(angle));
                Vector3 expected = new Vector3(MathF.Cos(angle) * input.x + MathF.Sin(angle) * input.y,
                    0, -MathF.Sin(angle) * input.x + MathF.Cos(angle) * input.y).normalized;
                Near(KinMotionMath.Direction(input, viewForward, viewRight, Vector3.right, Vector3.forward, false),
                    expected, "Screen direction must not depend on Kin's current heading.");
            }
        }
        Check(!KinMotionMath.TurnWithMovement(new(0, 0), false), "Idle input must not choose a facing.");
        Near(KinMotionMath.Direction(new(0, 1), Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, false),
            Vector3.forward, "Degenerate camera basis must have a stable fallback.");
        Vector3 analog = KinMotionMath.Direction(new(0, .4f), Vector3.forward, Vector3.right,
            Vector3.forward, Vector3.right, false);
        Check(MathF.Abs(analog.magnitude - .4f) < .001f, "Joystick intensity must remain analog.");
        foreach (int hz in new[] { 30, 60, 120 })
        {
            Vector3 velocity = Vector3.zero;
            for (int frame = 0; frame < hz; frame++)
                velocity = KinMotionMath.ApproachVelocity(velocity, Vector3.forward, 7, 1f / hz);
            Near(velocity, Vector3.forward * 7, "Acceleration must reach the requested speed.");
            for (int frame = 0; frame < hz; frame++)
                velocity = KinMotionMath.ApproachVelocity(velocity, Vector3.zero, 7, 1f / hz);
            Near(velocity, Vector3.zero, "Braking must stop the gait.");
        }
        Near(KinMotionMath.ApproachVelocity(Vector3.right, Vector3.forward, 7, 0),
            Vector3.right, "Paused motion must remain unchanged.");

        Check(KinPoseMath.Swing(0) == 0 && KinPoseMath.Swing(1) == 0, "Strike must return to rest.");
        Check(KinPoseMath.Anticipation(0) == 0 && KinPoseMath.Anticipation(1) == 0,
            "Preparation must return to rest.");
        Check(KinPoseMath.Swing(.18f) == 0 && KinPoseMath.Anticipation(.18f) > .99f,
            "Preparation must precede impact.");
        Check(KinPoseMath.Swing(.46f) > .99f, "Strike must reach its readable impact pose.");
        foreach (float p in new[] { 0f, .18f, .46f, 1f })
        {
            float left = (KinPoseMath.Swing(p) - KinPoseMath.Swing(p - .0001f)) / .0001f;
            float right = (KinPoseMath.Swing(p + .0001f) - KinPoseMath.Swing(p)) / .0001f;
            Check(MathF.Abs(left - right) < .03f, "Strike joins must have a continuous velocity.");
        }
        foreach (string path in new[] { "Models/DemonSono", "Models/DemonPoerix", "Models/DemonFlamurk" })
        {
            Check(!CharacterMotionMath.UsesArticulatedRig(path), "Demon faces must bypass human skin weights.");
            var profile = CharacterMotionMath.ProfileFor(path);
            for (int frame = 0; frame <= 120; frame++)
            {
                float p = frame / 120f;
                CharacterBodyPose pose = CharacterMotionMath.Evaluate(profile, frame * .1f,
                    frame * .023f, 1f, true, p);
                Check(float.IsFinite(pose.Offset.magnitude) && pose.Offset.magnitude < .25f,
                    "Demon lunge and locomotion must remain bounded.");
                Check(pose.Offset.y >= 0, "Body animation must not move its foot pivot under the ground.");
                Check(pose.Euler.magnitude < 20f, "Demon body rotations must preserve a readable silhouette.");
            }
            CharacterBodyPose resting = CharacterMotionMath.Evaluate(profile, 1f, .6f, .7f, false, 0);
            CharacterBodyPose recovered = CharacterMotionMath.Evaluate(profile, 1f, .6f, .7f, true, 1);
            Near(recovered.Offset, resting.Offset, "Attack recovery must return to the current locomotion pose.");
            Near(recovered.Euler, resting.Euler, "Attack recovery must not snap back to a stale orientation.");
        }
        Check(CharacterMotionMath.UsesArticulatedRig("Models/Kin"), "Kin must keep his animated arms and equipment.");
        Check(CharacterMotionMath.ProfileFor("Models/DemonSono") != CharacterMotionMath.ProfileFor("Models/DemonPoerix") &&
            CharacterMotionMath.ProfileFor("Models/DemonPoerix") != CharacterMotionMath.ProfileFor("Models/DemonFlamurk"),
            "The three demons must keep distinct movement profiles.");
        Console.WriteLine($"PASS: {checks} actual-source movement and animation math checks; native Unity validation pending.");
    }
}
