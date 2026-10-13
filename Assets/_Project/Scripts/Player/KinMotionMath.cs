using UnityEngine;

namespace CatsVsDemons.Player
{
    internal static class KinMotionMath
    {
        public static Vector3 Direction(Vector2 input, Vector3 viewForward, Vector3 viewRight,
            Vector3 facingForward, Vector3 facingRight, bool firstPerson, bool combat)
        {
            // A backward command retreats along Kin's facing, without a 180-degree turn.
            bool relativeToKin = firstPerson || combat || input.y < -.05f;
            Vector3 direction = (relativeToKin ? facingForward : viewForward) * input.y +
                (relativeToKin ? facingRight : viewRight) * input.x;
            direction.y = 0;
            return Vector3.ClampMagnitude(direction, 1);
        }
        public static Vector3 ApproachVelocity(Vector3 current, Vector3 direction, float speed, float dt)
        {
            if (dt <= 0) return current;
            float response = direction.sqrMagnitude > .0001f ? 42f : 54f;
            return Vector3.MoveTowards(current, direction * speed, response * dt);
        }
        public static bool TurnWithMovement(Vector2 input, bool firstPerson, bool combat) =>
            !firstPerson && !combat && input.y >= -.05f && input.sqrMagnitude > .001f;
    }
}
