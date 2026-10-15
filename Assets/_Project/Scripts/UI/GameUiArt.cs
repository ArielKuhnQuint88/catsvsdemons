using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.UI
{
    internal enum UiIcon { Pause, Book, Home, Coin, Special, Check, Chevron, Isometric, FirstPerson, Shop, Close }

    /// <summary>Small, antialiased native sprites shared by every menu. No asset downloads or per-frame textures.</summary>
    internal static class GameUiArt
    {
        private static readonly Dictionary<string, Sprite> cache = new();
        private static readonly List<Object> owned = new();
        public static Sprite Rounded => Surface("Rounded", false, false);
        public static Sprite Button => Surface("Button", true, false);
        public static Sprite Contour => Surface("Contour", false, true);
        public static Sprite Circle => Disc(false);
        public static Sprite Ring => Disc(true);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach (Object item in owned) if (item != null) Object.Destroy(item);
            owned.Clear(); cache.Clear();
        }

        private static Sprite Surface(string key, bool shaded, bool outline)
        {
            if (cache.TryGetValue(key, out Sprite existing) && existing != null) return existing;
            const int size = 96;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = RoundedDistance(new Vector2(x + .5f, y + .5f), new Vector2(48, 48), new Vector2(47, 47), 18f);
                float alpha = Mathf.Clamp01(.5f - distance);
                if (outline) alpha *= Mathf.Clamp01(distance + 2.3f);
                float light = shaded ? Mathf.Lerp(.81f, 1f, y / 95f) : 1f;
                pixels[y * size + x] = new Color(light, light, light, alpha);
            }
            return Save(key, pixels, size, new Vector4(22, 22, 22, 22));
        }

        private static float RoundedDistance(Vector2 point, Vector2 center, Vector2 half, float radius)
        {
            Vector2 q = new(Mathf.Abs(point.x - center.x) - half.x + radius,
                Mathf.Abs(point.y - center.y) - half.y + radius);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
        }

        private static Sprite Disc(bool ring)
        {
            string key = ring ? "Ring" : "Circle";
            if (cache.TryGetValue(key, out Sprite existing) && existing != null) return existing;
            const int size = 128;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x - 63.5f, y - 63.5f).magnitude;
                float alpha = Mathf.Clamp01(62f - radius);
                if (ring) alpha *= Mathf.Clamp01(radius - 57f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            return Save(key, pixels, size, Vector4.zero);
        }

        public static Sprite Symbol(UiIcon icon)
        {
            string key = "Symbol " + icon;
            if (cache.TryGetValue(key, out Sprite existing) && existing != null) return existing;
            const int size = 96;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new((x + .5f) / size, (y + .5f) / size);
                float distance = IconDistance(icon, p);
                float alpha = Mathf.Clamp01(.5f - distance * size);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            return Save(key, pixels, size, Vector4.zero);
        }

        private static float Line(Vector2 p, float ax, float ay, float bx, float by, float width = .027f)
        {
            Vector2 a = new(ax, ay), b = new(bx, by), ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.00001f, ab.sqrMagnitude));
            return (p - a - ab * t).magnitude - width;
        }
        private static float RingAt(Vector2 p, float x, float y, float radius, float width = .025f) =>
            Mathf.Abs((p - new Vector2(x, y)).magnitude - radius) - width;
        private static float Box(Vector2 p, float x, float y, float w, float h, float r = .025f) =>
            RoundedDistance(p, new Vector2(x, y), new Vector2(w, h), r);
        private static float IconDistance(UiIcon icon, Vector2 p)
        {
            float d = 10f;
            switch (icon)
            {
                case UiIcon.Pause:
                    return Mathf.Min(Box(p, .36f, .5f, .055f, .25f), Box(p, .64f, .5f, .055f, .25f));
                case UiIcon.Close:
                    return Mathf.Min(Line(p, .26f, .26f, .74f, .74f), Line(p, .26f, .74f, .74f, .26f));
                case UiIcon.Check:
                    return Mathf.Min(Line(p, .2f, .48f, .41f, .28f, .045f), Line(p, .41f, .28f, .8f, .76f, .045f));
                case UiIcon.Chevron:
                    return Mathf.Min(Line(p, .38f, .24f, .65f, .5f), Line(p, .65f, .5f, .38f, .76f));
                case UiIcon.Coin:
                    return Mathf.Min(RingAt(p, .5f, .5f, .3f, .036f), Mathf.Abs(Box(p, .5f, .5f, .085f, .085f, .003f)) - .02f);
                case UiIcon.Special:
                    d = Line(p, .26f, .26f, .7f, .74f, .06f);
                    d = Mathf.Min(d, Line(p, .21f, .21f, .36f, .38f, .035f));
                    d = Mathf.Min(d, Line(p, .22f, .44f, .47f, .22f, .04f));
                    return Mathf.Min(d, Line(p, .69f, .76f, .79f, .81f, .033f));
                case UiIcon.Book:
                    d = Line(p, .5f, .2f, .5f, .77f);
                    d = Mathf.Min(d, Line(p, .5f, .77f, .21f, .83f));
                    d = Mathf.Min(d, Line(p, .21f, .83f, .21f, .3f));
                    d = Mathf.Min(d, Line(p, .21f, .3f, .5f, .2f));
                    d = Mathf.Min(d, Line(p, .5f, .77f, .79f, .83f));
                    d = Mathf.Min(d, Line(p, .79f, .83f, .79f, .3f));
                    return Mathf.Min(d, Line(p, .79f, .3f, .5f, .2f));
                case UiIcon.Home:
                case UiIcon.Shop:
                    d = Line(p, .17f, .58f, .5f, .82f, .036f);
                    d = Mathf.Min(d, Line(p, .5f, .82f, .83f, .58f, .036f));
                    d = Mathf.Min(d, Line(p, .25f, .57f, .25f, .23f));
                    d = Mathf.Min(d, Line(p, .75f, .57f, .75f, .23f));
                    d = Mathf.Min(d, Line(p, .25f, .23f, .75f, .23f));
                    return Mathf.Min(d, Mathf.Abs(Box(p, .5f, .37f, .09f, .14f, .003f)) - .02f);
                case UiIcon.FirstPerson:
                    d = RingAt(p, .5f, .5f, .21f);
                    d = Mathf.Min(d, Line(p, .5f, .14f, .5f, .34f));
                    d = Mathf.Min(d, Line(p, .5f, .66f, .5f, .86f));
                    d = Mathf.Min(d, Line(p, .14f, .5f, .34f, .5f));
                    return Mathf.Min(d, Line(p, .66f, .5f, .86f, .5f));
                default:
                    d = Line(p, .5f, .81f, .16f, .6f);
                    d = Mathf.Min(d, Line(p, .16f, .6f, .5f, .39f));
                    d = Mathf.Min(d, Line(p, .5f, .39f, .84f, .6f));
                    d = Mathf.Min(d, Line(p, .84f, .6f, .5f, .81f));
                    d = Mathf.Min(d, Line(p, .16f, .44f, .5f, .23f));
                    return Mathf.Min(d, Line(p, .5f, .23f, .84f, .44f));
            }
        }

        private static Sprite Save(string key, Color32[] pixels, int size, Vector4 border)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "Jade UI " + key, filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            sprite.name = texture.name; sprite.hideFlags = HideFlags.HideAndDontSave;
            owned.Add(sprite); owned.Add(texture); cache[key] = sprite;
            return sprite;
        }
    }
}
