using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    public static class GardenPathGeometry
    {
        public static Vector3[] Smooth(Vector3[] source, int subdivisions = 3)
        {
            List<Vector3> clean = new();
            foreach (Vector3 point in source)
            {
                Vector3 flat = new(point.x, 0f, point.z);
                if (clean.Count == 0 || (flat - clean[clean.Count - 1]).sqrMagnitude > 0.0001f)
                    clean.Add(flat);
            }
            if (clean.Count < 2) return clean.ToArray();
            subdivisions = Mathf.Clamp(subdivisions, 1, 8);
            List<Vector3> result = new();
            for (int segment = 0; segment < clean.Count - 1; segment++)
            {
                Vector3 p0 = clean[Mathf.Max(0, segment - 1)];
                Vector3 p1 = clean[segment];
                Vector3 p2 = clean[segment + 1];
                Vector3 p3 = clean[Mathf.Min(clean.Count - 1, segment + 2)];
                for (int step = 0; step < subdivisions; step++)
                {
                    float t = step / (float)subdivisions;
                    Vector3 point = 0.5f * ((2f * p1) + (-p0 + p2) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                        (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
                    if (result.Count == 0 || (point - result[result.Count - 1]).sqrMagnitude > 0.00001f)
                        result.Add(point);
                }
            }
            result.Add(clean[clean.Count - 1]);
            return result.ToArray();
        }

        public static Mesh Ribbon(Vector3[] points, float width, float height)
        {
            Mesh mesh = new() { name = "Continuous Garden Path" };
            if (points.Length < 2) return mesh;
            Vector3[] vertices = new Vector3[points.Length * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[(points.Length - 1) * 6];
            float distance = 0f;
            for (int index = 0; index < points.Length; index++)
            {
                Vector3 previous = points[Mathf.Max(0, index - 1)];
                Vector3 next = points[Mathf.Min(points.Length - 1, index + 1)];
                Vector3 before = points[index] - previous;
                Vector3 after = next - points[index];
                before.y = after.y = 0f;
                if (before.sqrMagnitude < 0.00001f) before = after;
                if (after.sqrMagnitude < 0.00001f) after = before;
                if (before.sqrMagnitude < 0.00001f) before = after = Vector3.forward;
                Vector3 normalBefore = Vector3.Cross(Vector3.up, before.normalized);
                Vector3 normalAfter = Vector3.Cross(Vector3.up, after.normalized);
                Vector3 miter = normalBefore + normalAfter;
                if (miter.sqrMagnitude < 0.00001f) miter = normalAfter;
                miter.Normalize();
                float denominator = Mathf.Max(0.6f, Mathf.Abs(Vector3.Dot(miter, normalAfter)));
                Vector3 side = miter * (width * 0.5f / denominator);
                Vector3 center = new(points[index].x, height, points[index].z);
                vertices[index * 2] = center - side;
                vertices[index * 2 + 1] = center + side;
                if (index > 0) distance += Vector3.Distance(points[index - 1], points[index]);
                uv[index * 2] = new Vector2(0f, distance);
                uv[index * 2 + 1] = new Vector2(width, distance);
                colors[index * 2] = colors[index * 2 + 1] = Color.white;
                if (index == points.Length - 1) continue;
                int t = index * 6;
                int v = index * 2;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            // Very tight inner bends can fold a strip triangle. Keep every floor
            // face upward; world-aligned paving remains consistent at overlaps.
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]];
                Vector3 b = vertices[triangles[t + 1]];
                Vector3 c = vertices[triangles[t + 2]];
                if (Vector3.Cross(b - a, c - a).y < 0f)
                    (triangles[t + 1], triangles[t + 2]) = (triangles[t + 2], triangles[t + 1]);
            }
            if (vertices.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static float DistanceToRoutes(Vector3 point, IReadOnlyList<Vector3[]> routes)
        {
            float closest = float.MaxValue;
            foreach (Vector3[] route in routes)
            for (int index = 0; index < route.Length - 1; index++)
            {
                Vector3 start = route[index];
                Vector3 delta = route[index + 1] - start;
                delta.y = 0f;
                Vector3 offset = point - start;
                offset.y = 0f;
                float t = delta.sqrMagnitude < 0.00001f ? 0f :
                    Mathf.Clamp01(Vector3.Dot(offset, delta) / delta.sqrMagnitude);
                closest = Mathf.Min(closest, (offset - delta * t).sqrMagnitude);
            }
            return Mathf.Sqrt(closest);
        }
    }
}
