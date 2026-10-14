using UnityEngine;

namespace CatsVsDemons.Player
{
    internal static class KinMotionMath
    {
        public static Vector3 Direction(Vector2 input, Vector3 viewForward, Vector3 viewRight,
            Vector3 facingForward, Vector3 facingRight, bool firstPerson)
        {
            // Isometric input always follows the camera, even when Kin faces an enemy.
            Vector3 forward = Vector3.ProjectOnPlane(firstPerson ? facingForward : viewForward, Vector3.up);
            if (forward.sqrMagnitude < .0001f)
            {
                Vector3 fallbackRight = Vector3.ProjectOnPlane(firstPerson ? facingRight : viewRight, Vector3.up);
                forward = Vector3.Cross(fallbackRight, Vector3.up);
            }
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            forward = forward.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = forward * input.y + right * input.x;
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
