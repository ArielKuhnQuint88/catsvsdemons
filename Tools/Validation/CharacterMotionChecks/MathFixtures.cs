// Minimal math API for testing the actual gameplay helpers without a Unity license.
// These fixtures do not simulate the engine, skinning, physics or rendering.
namespace UnityEngine
{
    public readonly struct Vector2(float x, float y)
    {
        public readonly float x = x, y = y;
        public float sqrMagnitude => x * x + y * y;
    }

    public readonly struct Vector3(float x, float y, float z)
    {
        public readonly float x = x, y = y, z = z;
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 1e-6f ? this / magnitude : zero;
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 right => new(1, 0, 0);
        public static Vector3 forward => new(0, 0, 1);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.x * b, a.y * b, a.z * b);
        public static Vector3 operator /(Vector3 a, float b) => a * (1f / b);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new(a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 ProjectOnPlane(Vector3 a, Vector3 normal) => a - normal * Dot(a, normal);
        public static Vector3 ClampMagnitude(Vector3 a, float max) => a.magnitude > max ? a.normalized * max : a;
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float distance) =>
            (target - current).magnitude <= distance ? target : current + (target - current).normalized * distance;
    }

    public static class Mathf
    {
        public static float Clamp01(float value) => Math.Clamp(value, 0, 1);
        public static float Sin(float value) => MathF.Sin(value);
        public static float Cos(float value) => MathF.Cos(value);
    }
}
