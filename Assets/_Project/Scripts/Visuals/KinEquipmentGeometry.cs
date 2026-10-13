using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Visuals
{
    public enum KinEquipmentAnchor { Pelvis, Chest, Head, LeftArm, RightArm, LeftHand, RightHand, LeftLeg, RightLeg, LeftFoot, RightFoot }

    public sealed class KinEquipmentPart
    {
        public string Name;
        public KinEquipmentAnchor Anchor;
        public Mesh Mesh;
        public bool Glass;
        public float Sway;
        public Vector3 Pivot;
    }

    // All designs use Kin's normalized, Y-up rest pose: feet=0, ear tips=1, front=+Z.
    public sealed class KinEquipmentGeometry
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<Color> colors = new();
        private readonly List<Vector2> uv = new();
        private readonly List<Vector2> surfaces = new();
        private readonly List<int> indices = new();
        public int VertexCount => vertices.Count;

        private void Vertex(Vector3 p, Vector3 n, Color tint, float metal, float glow)
        {
            vertices.Add(p); normals.Add(n.normalized);
            colors.Add(QualitySettings.activeColorSpace == ColorSpace.Linear ? tint.linear : tint);
            uv.Add(new Vector2(p.x * 4f, p.y * 4f + p.z));
            surfaces.Add(new Vector2(metal, glow));
            indices.Add(vertices.Count - 1);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color, float metal = 0, float glow = 0,
            bool twoSided = false)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            if (n.sqrMagnitude < 0.5f) return;
            Vertex(a, n, color, metal, glow); Vertex(b, n, color, metal, glow); Vertex(c, n, color, metal, glow);
            if (twoSided) { Vertex(c, -n, color, metal, glow); Vertex(b, -n, color, metal, glow); Vertex(a, -n, color, metal, glow); }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, float metal = 0,
            float glow = 0, bool twoSided = false)
        {
            Triangle(a, b, c, color, metal, glow, twoSided); Triangle(a, c, d, color, metal, glow, twoSided);
        }

        public void Ellipsoid(Vector3 center, Vector3 radius, Color color, float metal = 0, float glow = 0,
            int sides = 12, int rings = 6)
        {
            if (Application.isMobilePlatform) { sides = Mathf.Min(sides, 10); rings = Mathf.Min(rings, 5); }
            Vector3 Point(int r, int s)
            {
                float t = Mathf.PI * r / rings, a = Mathf.PI * 2f * s / sides;
                return center + Vector3.Scale(new Vector3(Mathf.Sin(t) * Mathf.Cos(a), Mathf.Cos(t), Mathf.Sin(t) * Mathf.Sin(a)), radius);
            }
            void Smooth(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 Normal(Vector3 p) => new Vector3((p.x - center.x) / (radius.x * radius.x),
                    (p.y - center.y) / (radius.y * radius.y), (p.z - center.z) / (radius.z * radius.z)).normalized;
                if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-15f) return;
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), Normal(a)) < 0) (b, c) = (c, b);
                Vertex(a, Normal(a), color, metal, glow); Vertex(b, Normal(b), color, metal, glow); Vertex(c, Normal(c), color, metal, glow);
            }
            for (int r = 0; r < rings; r++) for (int s = 0; s < sides; s++)
            {
                Vector3 a = Point(r, s), b = Point(r + 1, s), c = Point(r + 1, s + 1), d = Point(r, s + 1);
                Smooth(a, b, c); Smooth(a, c, d);
            }
        }

        public void Tube(IReadOnlyList<Vector3> points, float radius, Color color, float metal = 0,
            float glow = 0, int sides = 7)
        {
            if (points.Count < 2) return;
            if (Application.isMobilePlatform) sides = Mathf.Min(sides, 6);
            bool closed = (points[0] - points[points.Count - 1]).sqrMagnitude < 0.00000001f;
            Vector3 Ring(int i, int s)
            {
                Vector3 tangent = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(0, i - 1)];
                if (closed && (i == 0 || i == points.Count - 1)) tangent = points[1] - points[points.Count - 2];
                Vector3 right = Vector3.Cross(tangent.normalized, Mathf.Abs(tangent.normalized.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, tangent.normalized).normalized;
                float a = s * Mathf.PI * 2f / sides;
                return points[i] + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
            }
            for (int i = 0; i < points.Count - 1; i++) for (int s = 0; s < sides; s++)
                Quad(Ring(i, s), Ring(i + 1, s), Ring(i + 1, s + 1), Ring(i, s + 1), color, metal, glow);
            for (int s = 0; s < sides && !closed; s++)
            {
                Triangle(points[0], Ring(0, s), Ring(0, s + 1), color, metal, glow);
                Triangle(points[points.Count - 1], Ring(points.Count - 1, s + 1), Ring(points.Count - 1, s), color, metal, glow);
            }
        }

        public void Ring(Vector3 center, float xRadius, float yRadius, float thickness, Color color,
            float metal = 0, float glow = 0, bool horizontal = false, int steps = 24)
        {
            if (Application.isMobilePlatform) steps = Mathf.Min(steps, 20);
            List<Vector3> points = new();
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                points.Add(center + (horizontal ? new Vector3(Mathf.Sin(a) * xRadius, 0, Mathf.Cos(a) * yRadius)
                    : new Vector3(Mathf.Cos(a) * xRadius, Mathf.Sin(a) * yRadius, 0)));
            }
            Tube(points, thickness, color, metal, glow, 6);
        }

        public void Shell(Vector3 center, float[] heights, Vector2[] radii, Color color,
            float startAngle = 0, float endAngle = 360, int sides = 20)
        {
            Vector3 Point(int row, int side)
            {
                float a = Mathf.Lerp(startAngle, endAngle, side / (float)sides) * Mathf.Deg2Rad;
                return center + new Vector3(Mathf.Sin(a) * radii[row].x, heights[row], Mathf.Cos(a) * radii[row].y);
            }
            for (int row = 0; row < heights.Length - 1; row++) for (int side = 0; side < sides; side++)
                Quad(Point(row, side), Point(row, side + 1), Point(row + 1, side + 1), Point(row + 1, side), color, 0, 0, true);
        }

        public void Band(Vector3 center, float xRadius, float zRadius, float height, Color color, float metal = 0)
        {
            Shell(center, new[] { -height * 0.5f, height * 0.5f }, new[] { new Vector2(xRadius, zRadius), new Vector2(xRadius, zRadius) }, color);
            Ring(center + Vector3.up * height * 0.5f, xRadius, zRadius, 0.004f, color, metal, 0, true);
            Ring(center - Vector3.up * height * 0.5f, xRadius, zRadius, 0.004f, color, metal, 0, true);
        }

        public void Sleeve(Vector3 start, Vector3 end, float startRadius, float endRadius, Color color)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.forward).normalized;
            Vector3 other = Vector3.Cross(direction, side).normalized;
            Vector3 Point(bool top, int i)
            {
                float a = i * Mathf.PI * 2f / 16;
                return (top ? start : end) + (side * Mathf.Cos(a) + other * Mathf.Sin(a)) * (top ? startRadius : endRadius);
            }
            for (int i = 0; i < 16; i++)
            {
                Quad(Point(true, i), Point(true, i + 1), Point(false, i + 1), Point(false, i), color, 0, 0, true);
                Quad(Point(false, i), Point(false, i + 1), Point(false, i + 1) - direction * 0.004f,
                    Point(false, i) - direction * 0.004f, Color.Lerp(color, Color.black, 0.2f), 0, 0, true);
            }
        }

        public void Panel(Vector3[] centers, float[] widths, Color color, float fold = 0.008f,
            float metal = 0, float glow = 0)
        {
            Vector3 Point(int row, int col) => centers[row] + new Vector3((col - 1) * widths[row] * 0.5f,
                0, col == 1 ? fold : 0);
            for (int row = 0; row < centers.Length - 1; row++) for (int col = 0; col < 2; col++)
                Quad(Point(row, col), Point(row + 1, col), Point(row + 1, col + 1), Point(row, col + 1), color, metal, glow, true);
        }

        public void Leaf(Vector3 root, Vector3 tip, float width, Color color, float glow = 0)
        {
            Vector3 axis = tip - root;
            Vector3 side = Vector3.Cross(axis.normalized, Vector3.forward).normalized * width;
            if (side.sqrMagnitude < 0.000001f) side = Vector3.right * width;
            Vector3 middle = Vector3.Lerp(root, tip, 0.48f), ridge = middle + Vector3.forward * width * 0.2f;
            Triangle(root, middle - side, ridge, color, 0.2f, glow, true);
            Triangle(middle - side, tip, ridge, color, 0.2f, glow, true);
            Triangle(tip, middle + side, ridge, Color.Lerp(color, Color.white, 0.15f), 0.2f, glow, true);
            Triangle(middle + side, root, ridge, color, 0.2f, glow, true);
        }

        public void Paw(Vector3 center, float size, Color color, float glow = 0)
        {
            Ellipsoid(center + Vector3.down * size * 0.16f, new Vector3(size * 0.34f, size * 0.25f, size * 0.09f), color, 0.8f, glow, 8, 4);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * size * 0.22f;
                float y = (i == 1 || i == 2 ? 0.28f : 0.12f) * size;
                Ellipsoid(center + new Vector3(x, y, 0), new Vector3(size * 0.105f, size * 0.14f, size * 0.08f), color, 0.8f, glow, 8, 4);
            }
        }

        public void Tassel(Vector3 top, float length, Color cord, Color gold)
        {
            Ellipsoid(top, new Vector3(0.008f, 0.013f, 0.008f), gold, 0.85f, 0, 8, 4);
            for (int i = -2; i <= 2; i++)
                Tube(new[] { top - Vector3.up * 0.01f, top + new Vector3(i * 0.004f, -length, 0.004f) }, 0.0028f, cord, 0, 0, 5);
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new() { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors);
            mesh.SetUVs(0, uv); mesh.SetUVs(1, surfaces); mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds(); return mesh;
        }
    }
}
