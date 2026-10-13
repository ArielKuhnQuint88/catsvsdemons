using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    /// <summary>Flat native UI geometry: roads only redraw on layout changes; actor dots update without a second camera.</summary>
    internal sealed class BattleMinimapGraphic : MaskableGraphic
    {
        public bool Terrain;
        private MinimapProjection projection;
        private readonly List<Vector3[]> roads = new();
        private readonly List<MinimapMarker> markers = new();
        private float clock;
        public void SetMap(MinimapProjection value, IReadOnlyList<Vector3[]> routes)
        {
            projection = value; roads.Clear();
            if (routes != null) foreach (Vector3[] route in routes) roads.Add(route);
            SetVerticesDirty();
        }
        public void SetActors(List<MinimapMarker> values, float time)
        {
            markers.Clear(); markers.AddRange(values); clock = time; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Vector2 size = rectTransform.rect.size;
            if (Terrain)
            {
                Color grid = new(.38f, .55f, .43f, .15f);
                for (int i = 1; i < 6; i++)
                {
                    float x = -size.x * .5f + size.x * i / 6, y = -size.y * .5f + size.y * i / 6;
                    Line(vh, new Vector2(x, -size.y * .5f), new Vector2(x, size.y * .5f), .6f, grid);
                    Line(vh, new Vector2(-size.x * .5f, y), new Vector2(size.x * .5f, y), .6f, grid);
                }
                foreach (Vector3[] route in roads)
                    for (int i = 1; i < route.Length; i++)
                        Line(vh, projection.Project(route[i - 1], size), projection.Project(route[i], size), 2.4f, new Color(.79f, .78f, .59f, .78f));
                return;
            }
            foreach (MinimapMarker mark in markers)
            {
                Vector2 p = projection.Project(mark.Position, size);
                if (mark.Kind == MinimapKind.Kin)
                {
                    Vector2 direction = new(mark.Forward.x, mark.Forward.z);
                    if (direction.sqrMagnitude < .001f) direction = Vector2.up;
                    direction.Normalize(); Vector2 side = new(-direction.y, direction.x);
                    Disc(vh, p, 7.4f, new Color(.035f, .08f, .055f, .9f));
                    Triangle(vh, p + direction * 6.8f, p - direction * 4.5f + side * 4.8f, p - direction * 4.5f - side * 4.8f, RuntimeUiFactory.Paper);
                }
                else if (mark.Kind == MinimapKind.Demon)
                { Disc(vh, p, 3.4f, RuntimeUiFactory.Ink); Disc(vh, p, 2.7f, RuntimeUiFactory.Coral); }
                else if (mark.Kind == MinimapKind.Home)
                {
                    Quad(vh, p + new Vector2(-5,-4), p + new Vector2(-5,4), p + new Vector2(5,4), p + new Vector2(5,-4), RuntimeUiFactory.Paper);
                    Triangle(vh, p + new Vector2(-7,4), p + new Vector2(0,9), p + new Vector2(7,4), RuntimeUiFactory.Gold);
                }
                else
                {
                    if (mark.UnderAttack)
                    {
                        float pulse = .5f + Mathf.Sin(clock * 9) * .5f;
                        Ring(vh, p, 7 + pulse * 3, 1.5f, new Color(1, .35f, .23f, .7f + pulse * .3f));
                    }
                    Color tint = mark.UnderAttack ? new Color(1, .39f, .23f) : mark.Color;
                    Quad(vh, p + Vector2.up * 4.7f, p + Vector2.right * 4.7f, p - Vector2.up * 4.7f, p - Vector2.right * 4.7f, tint);
                }
            }
        }
        private static void Triangle(VertexHelper h, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = h.currentVertCount;
            h.AddVert(a, color, Vector2.zero); h.AddVert(b, color, Vector2.zero); h.AddVert(c, color, Vector2.zero);
            h.AddTriangle(start, start + 1, start + 2);
        }
        private static void Quad(VertexHelper h, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        { Triangle(h, a, b, c, color); Triangle(h, a, c, d, color); }
        private static void Line(VertexHelper h, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < .00001f) return;
            Vector2 normal = new Vector2(-dir.y, dir.x).normalized * width * .5f;
            Quad(h, a - normal, a + normal, b + normal, b - normal, color);
        }
        private static void Disc(VertexHelper h, Vector2 p, float radius, Color color)
        {
            const int sides = 10;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Triangle(h, p, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, p + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
            }
        }
        private static void Ring(VertexHelper h, Vector2 p, float radius, float width, Color color)
        {
            const int sides = 20;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector2 va = new(Mathf.Cos(a), Mathf.Sin(a)), vb = new(Mathf.Cos(b), Mathf.Sin(b));
                Quad(h, p + va * radius, p + va * (radius - width), p + vb * (radius - width), p + vb * radius, color);
            }
        }
    }
}
