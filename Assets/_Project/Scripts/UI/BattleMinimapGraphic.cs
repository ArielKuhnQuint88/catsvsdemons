using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    /// <summary>Native UI geometry: roads redraw on phase, zoom or focus changes; markers need no second camera.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
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
                        if (projection.ClipRoad(route[i - 1], route[i], size, out Vector2 a, out Vector2 b))
                        {
                            Line(vh, a, b, 5.8f, new Color(.035f, .09f, .06f, .75f));
                            Line(vh, a, b, 3.2f, new Color(.88f, .82f, .57f, .94f));
                        }
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
                    Disc(vh, p, 10.5f, new Color(.035f, .08f, .055f, .9f));
                    Triangle(vh, p + direction * 9.5f, p - direction * 6.3f + side * 6.7f, p - direction * 6.3f - side * 6.7f, RuntimeUiFactory.Paper);
                }
                else if (mark.Kind == MinimapKind.Demon)
                { Disc(vh, p, 5.3f, RuntimeUiFactory.Ink); Disc(vh, p, 4.3f, new Color(.94f, .39f, .29f)); }
                else if (mark.Kind == MinimapKind.Home)
                {
                    Quad(vh, p + new Vector2(-7,-5.6f), p + new Vector2(-7,5.6f), p + new Vector2(7,5.6f), p + new Vector2(7,-5.6f), RuntimeUiFactory.Paper);
                    Triangle(vh, p + new Vector2(-9.8f,5.6f), p + new Vector2(0,12.6f), p + new Vector2(9.8f,5.6f), RuntimeUiFactory.Gold);
                }
                else
                {
                    if (mark.UnderAttack)
                    {
                        float pulse = .5f + Mathf.Sin(clock * 9) * .5f;
                        Ring(vh, p, 9.5f + pulse * 4, 2f, new Color(1, .35f, .23f, .7f + pulse * .3f));
                    }
                    Color tint = mark.UnderAttack ? new Color(1, .39f, .23f) : mark.Color;
                    Quad(vh, p + Vector2.up * 6.5f, p + Vector2.right * 6.5f, p - Vector2.up * 6.5f, p - Vector2.right * 6.5f, tint);
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
