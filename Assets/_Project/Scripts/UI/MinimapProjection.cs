using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.UI
{
    internal struct MinimapProjection
    {
        public Vector2 Center;
        public Vector2 WorldSize;
        private const float MarkerMargin = 14f;
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
            Vector2 point = ProjectUnclamped(world, pixels);
            point.x = Mathf.Clamp(point.x, -pixels.x * .5f + MarkerMargin, pixels.x * .5f - MarkerMargin);
            point.y = Mathf.Clamp(point.y, -pixels.y * .5f + MarkerMargin, pixels.y * .5f - MarkerMargin);
            return point;
        }
        public MinimapProjection WithZoom(float zoom, Vector3 focus)
        {
            zoom = Mathf.Clamp(zoom, 1, 3);
            return new MinimapProjection {
                Center = Vector2.Lerp(Center, new Vector2(focus.x, focus.z), Mathf.Clamp01(zoom - 1)),
                WorldSize = WorldSize / zoom
            };
        }
        public void KeepVisible(Vector3 target, Vector2 pixels)
        {
            float scale = PixelScale(pixels);
            Vector2 limit = new Vector2(pixels.x - MarkerMargin * 2, pixels.y - MarkerMargin * 2) / scale * .3f;
            Vector2 offset = new Vector2(target.x, target.z) - Center;
            Center += new Vector2(offset.x - Mathf.Clamp(offset.x, -limit.x, limit.x),
                offset.y - Mathf.Clamp(offset.y, -limit.y, limit.y));
        }
        private float PixelScale(Vector2 pixels) => Mathf.Min(Mathf.Max(1, pixels.x - MarkerMargin * 2) / Mathf.Max(1, WorldSize.x),
            Mathf.Max(1, pixels.y - MarkerMargin * 2) / Mathf.Max(1, WorldSize.y));
        private Vector2 ProjectUnclamped(Vector3 world, Vector2 pixels) =>
            new Vector2(world.x - Center.x, world.z - Center.y) * PixelScale(pixels);

        public bool ClipRoad(Vector3 start, Vector3 end, Vector2 pixels, out Vector2 a, out Vector2 b)
        {
            a = ProjectUnclamped(start, pixels); b = ProjectUnclamped(end, pixels);
            Vector2 delta = b - a;
            float enter = 0, leave = 1;
            float halfWidth = Mathf.Max(0, pixels.x * .5f - 2), halfHeight = Mathf.Max(0, pixels.y * .5f - 2);
            if (!Clip(-delta.x, a.x + halfWidth, ref enter, ref leave) ||
                !Clip(delta.x, halfWidth - a.x, ref enter, ref leave) ||
                !Clip(-delta.y, a.y + halfHeight, ref enter, ref leave) ||
                !Clip(delta.y, halfHeight - a.y, ref enter, ref leave)) return false;
            b = a + delta * leave; a += delta * enter;
            return true;
        }
        private static bool Clip(float direction, float distance, ref float enter, ref float leave)
        {
            if (Mathf.Abs(direction) < .00001f) return distance >= 0;
            float ratio = distance / direction;
            if (direction < 0) { if (ratio > leave) return false; enter = Mathf.Max(enter, ratio); }
            else { if (ratio < enter) return false; leave = Mathf.Min(leave, ratio); }
            return true;
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
