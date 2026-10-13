using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    /// <summary>Small authored shapes collected into one colored mesh per scenery group.</summary>
    public sealed class GardenMeshBuilder
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<Color> colors = new();
        private readonly List<int> triangles = new();
        private readonly bool linearColors = QualitySettings.activeColorSpace == ColorSpace.Linear;
        public int VertexCount => vertices.Count;

        public void ScaleAround(Vector3 origin, Vector3 scale)
        {
            for (int index = 0; index < vertices.Count; index++)
            {
                vertices[index] = origin + Vector3.Scale(vertices[index] - origin, scale);
                Vector3 normal = normals[index];
                normals[index] = new Vector3(normal.x / scale.x, normal.y / scale.y, normal.z / scale.z).normalized;
            }
        }

        public void Append(GardenMeshBuilder other)
        {
            int offset = vertices.Count;
            vertices.AddRange(other.vertices); normals.AddRange(other.normals);
            uvs.AddRange(other.uvs); colors.AddRange(other.colors);
            foreach (int index in other.triangles) triangles.Add(offset + index);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color, float wind = 0f)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (normal.sqrMagnitude < 0.5f) return;
            int start = vertices.Count;
            // Vertex streams do not get the sRGB conversion applied to material colors.
            Color linear = linearColors ? color.linear : color;
            Color tint = new(linear.r, linear.g, linear.b, wind);
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            normals.Add(normal); normals.Add(normal); normals.Add(normal);
            colors.Add(tint); colors.Add(tint); colors.Add(tint);
            uvs.Add(new Vector2(a.x, a.z)); uvs.Add(new Vector2(b.x, b.z)); uvs.Add(new Vector2(c.x, c.z));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, float wind = 0f)
        {
            Triangle(a, b, c, color, wind); Triangle(a, c, d, color, wind);
        }

        public void Box(Vector3 center, Vector3 size, Color color, Quaternion rotation)
        {
            Vector3 half = size * 0.5f;
            Vector3[] p = new Vector3[8];
            for (int index = 0; index < 8; index++)
                p[index] = center + rotation * new Vector3((index & 1) == 0 ? -half.x : half.x,
                    (index & 2) == 0 ? -half.y : half.y, (index & 4) == 0 ? -half.z : half.z);
            Quad(p[2], p[6], p[7], p[3], color);
            Quad(p[0], p[1], p[5], p[4], color);
            Quad(p[0], p[2], p[3], p[1], color);
            Quad(p[4], p[5], p[7], p[6], color);
            Quad(p[0], p[4], p[6], p[2], color);
            Quad(p[1], p[3], p[7], p[5], color);
        }

        public void Box(Vector3 center, Vector3 size, Color color) => Box(center, size, color, Quaternion.identity);

        public void Stem(Vector3 start, Vector3 end, float radius, Color color, float topRadius = -1f, int sides = 7, float wind = 0f)
        {
            if (topRadius < 0f) topRadius = radius * 0.72f;
            Vector3 direction = end - start;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            for (int side = 0; side < sides; side++)
            {
                float a = side * Mathf.PI * 2f / sides;
                float b = (side + 1) * Mathf.PI * 2f / sides;
                Vector3 r0 = rotation * new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 r1 = rotation * new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b));
                Vector3 bottom0 = start + r0 * radius;
                Vector3 bottom1 = start + r1 * radius;
                Vector3 top0 = end + r0 * topRadius;
                Vector3 top1 = end + r1 * topRadius;
                Quad(bottom0, top0, top1, bottom1, color, wind);
                Triangle(end, top1, top0, color, wind);
                Triangle(start, bottom0, bottom1, color, wind);
            }
        }

        public void Blob(Vector3 center, Vector3 size, Color color, int seed = 0, float wind = 0f)
        {
            bool small = size.sqrMagnitude < 1.7f;
            int rings = small ? 3 : 5;
            int sides = small ? 6 : 9;
            Vector3 Point(int ring, int side)
            {
                float theta = Mathf.PI * ring / rings;
                float phi = Mathf.PI * 2f * side / sides;
                if (ring == 0) return center + Vector3.up * size.y * 0.5f;
                if (ring == rings) return center - Vector3.up * size.y * 0.5f;
                // Periodic variation closes the seam and leaves both poles shared.
                float bump = 1f + Mathf.Sin(phi * 3f + ring * 1.43f + seed) * Mathf.Sin(theta) * 0.09f;
                Vector3 sphere = new(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                return center + Vector3.Scale(sphere * 0.5f * bump, size);
            }
            for (int ring = 0; ring < rings; ring++)
            for (int side = 0; side < sides; side++)
            {
                Color tint = Color.Lerp(color, Color.white, ((side + ring + seed) % 4) * 0.018f);
                Quad(Point(ring, side), Point(ring, side + 1), Point(ring + 1, side + 1), Point(ring + 1, side), tint, wind);
            }
        }

        public void Leaf(Vector3 basePoint, Vector3 tip, float width, Color color, float wind = 1f)
        {
            Vector3 axis = tip - basePoint;
            Vector3 side = Vector3.Cross(Vector3.up, axis).normalized * width;
            if (side.sqrMagnitude < 0.000001f) side = Vector3.right * width;
            Vector3 mid = Vector3.Lerp(basePoint, tip, 0.43f) + Vector3.up * width * 0.4f;
            Triangle(basePoint, mid + side, tip, color, wind);
            Triangle(basePoint, tip, mid - side, color, wind);
            Triangle(basePoint, tip, mid + side, color, wind);
            Triangle(basePoint, mid - side, tip, color, wind);
        }

        public void Roof(Vector3 center, float width, float depth, float height, Color color)
        {
            Vector3[][] rings = new Vector3[3][];
            for (int ring = 0; ring < 3; ring++)
            {
                float factor = ring == 0 ? 1f : ring == 1 ? 0.77f : 0.18f;
                float y = ring == 0 ? 0.1f : ring == 1 ? 0.34f : height;
                rings[ring] = new[] {
                    center + new Vector3(-width * factor * 0.5f, y, -depth * factor * 0.5f),
                    center + new Vector3(-width * factor * 0.5f, y, depth * factor * 0.5f),
                    center + new Vector3(width * factor * 0.5f, y, depth * factor * 0.5f),
                    center + new Vector3(width * factor * 0.5f, y, -depth * factor * 0.5f) };
            }
            for (int ring = 0; ring < 2; ring++)
            for (int side = 0; side < 4; side++)
            {
                int next = (side + 1) % 4;
                Quad(rings[ring][side], rings[ring][next], rings[ring + 1][next], rings[ring + 1][side], color);
            }
            Quad(rings[2][0], rings[2][1], rings[2][2], rings[2][3], color);
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new() { name = name };
            if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs);
            mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
