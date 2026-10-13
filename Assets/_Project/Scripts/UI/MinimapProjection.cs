using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.UI
{
    internal struct MinimapProjection
    {
        public Vector2 Center;
        public Vector2 WorldSize;
        public static MinimapProjection FromRoutes(IReadOnlyList<Vector3[]> routes, Vector3 home)
        {
            Vector2 min = new(home.x - 22, home.z - 18), max = new(home.x + 22, home.z + 24);
            if (routes != null) foreach (Vector3[] route in routes)
                foreach (Vector3 p in route)
                { min.x = Mathf.Min(min.x, p.x); min.y = Mathf.Min(min.y, p.z); max.x = Mathf.Max(max.x, p.x); max.y = Mathf.Max(max.y, p.z); }
            return new MinimapProjection { Center = (min + max) * .5f, WorldSize = max - min + new Vector2(10, 10) };
        }
        public Vector2 Project(Vector3 world, Vector2 pixels)
        {
            float scale = Mathf.Min(Mathf.Max(1, pixels.x - 14) / Mathf.Max(1, WorldSize.x), Mathf.Max(1, pixels.y - 14) / Mathf.Max(1, WorldSize.y));
            Vector2 point = new((world.x - Center.x) * scale, (world.z - Center.y) * scale);
            point.x = Mathf.Clamp(point.x, -pixels.x * .5f + 7, pixels.x * .5f - 7);
            point.y = Mathf.Clamp(point.y, -pixels.y * .5f + 7, pixels.y * .5f - 7);
            return point;
        }
    }
    internal enum MinimapKind { Kin, Demon, Tower, Home }
    internal struct MinimapMarker
    {
        public Vector3 Position, Forward;
        public MinimapKind Kind;
        public bool UnderAttack;
        public Color Color;
    }
}
