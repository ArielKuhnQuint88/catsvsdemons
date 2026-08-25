using UnityEngine;

namespace CatsVsDemons.UI
{
    internal static class ResponsiveGuiTheme
    {
        internal enum ButtonTone
        {
            Crimson,
            Azure,
            Gold,
            Ink
        }

        private static GUIStyle crimson;
        private static GUIStyle azure;
        private static GUIStyle gold;
        private static GUIStyle ink;

        public static bool IsMobile =>
            Application.isMobilePlatform ||
            SystemInfo.deviceType == DeviceType.Handheld;

        public static float LayoutScale => IsMobile
            ? Mathf.Clamp(Screen.height / 720f, 1f, 2f)
            : Mathf.Clamp(Screen.height / 1080f, 0.72f, 1f);

        public static bool Button(
            Rect area,
            string label,
            ButtonTone tone,
            int fontSize)
        {
            EnsureStyles();
            GUIStyle style = tone switch
            {
                ButtonTone.Crimson => crimson,
                ButtonTone.Azure => azure,
                ButtonTone.Gold => gold,
                _ => ink
            };
            style.fontSize = Mathf.Max(12, fontSize);

            Rect shadow = new Rect(
                area.x + 3f * LayoutScale,
                area.y + 5f * LayoutScale,
                area.width,
                area.height
            );
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.38f);
            GUI.DrawTexture(shadow, style.normal.background, ScaleMode.StretchToFill);
            GUI.color = previous;
            return GUI.Button(area, label, style);
        }

        private static void EnsureStyles()
        {
            if (crimson != null)
                return;

            crimson = CreateStyle(
                new Color(0.48f, 0.055f, 0.045f),
                new Color(0.19f, 0.018f, 0.025f),
                new Color(0.94f, 0.55f, 0.16f),
                Color.white
            );
            azure = CreateStyle(
                new Color(0.055f, 0.30f, 0.52f),
                new Color(0.018f, 0.08f, 0.18f),
                new Color(0.16f, 0.72f, 0.96f),
                Color.white
            );
            gold = CreateStyle(
                new Color(1f, 0.78f, 0.24f),
                new Color(0.68f, 0.32f, 0.055f),
                new Color(1f, 0.92f, 0.52f),
                new Color(0.12f, 0.055f, 0.018f)
            );
            ink = CreateStyle(
                new Color(0.16f, 0.13f, 0.22f),
                new Color(0.035f, 0.025f, 0.07f),
                new Color(0.62f, 0.46f, 0.82f),
                Color.white
            );
        }

        private static GUIStyle CreateStyle(
            Color top,
            Color bottom,
            Color border,
            Color text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                wordWrap = false,
                border = new RectOffset(18, 18, 16, 16),
                padding = new RectOffset(14, 14, 6, 6)
            };
            style.normal.background = CreateRoundedTexture(top, bottom, border);
            style.hover.background = CreateRoundedTexture(
                Color.Lerp(top, Color.white, 0.16f),
                Color.Lerp(bottom, Color.white, 0.08f),
                Color.Lerp(border, Color.white, 0.28f)
            );
            style.active.background = CreateRoundedTexture(
                Color.Lerp(top, Color.black, 0.2f),
                Color.Lerp(bottom, Color.black, 0.28f),
                border
            );
            style.normal.textColor = text;
            style.hover.textColor = Color.white;
            style.active.textColor = text;
            return style;
        }

        private static Texture2D CreateRoundedTexture(
            Color top,
            Color bottom,
            Color border)
        {
            const int width = 128;
            const int height = 56;
            const float radius = 14f;
            const float borderWidth = 3f;
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            )
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float blend = y / (height - 1f);
                Color fill = Color.Lerp(bottom, top, blend);
                for (int x = 0; x < width; x++)
                {
                    bool outer = InsideRounded(x, y, width, height, radius);
                    bool inner = InsideRounded(
                        x - borderWidth,
                        y - borderWidth,
                        width - borderWidth * 2f,
                        height - borderWidth * 2f,
                        radius - borderWidth
                    );
                    Color pixel = !outer
                        ? Color.clear
                        : !inner
                            ? border
                            : fill;
                    pixels[y * width + x] = pixel;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool InsideRounded(
            float x,
            float y,
            float width,
            float height,
            float radius)
        {
            if (x < 0f || y < 0f || x >= width || y >= height)
                return false;

            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float dx = Mathf.Max(
                Mathf.Abs(x - centerX) - (width * 0.5f - radius),
                0f
            );
            float dy = Mathf.Max(
                Mathf.Abs(y - centerY) - (height * 0.5f - radius),
                0f
            );
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
