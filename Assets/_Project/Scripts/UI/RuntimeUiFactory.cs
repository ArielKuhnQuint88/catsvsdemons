using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    internal sealed class RuntimeUiFactory
    {
        private readonly Font font = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf");
        private static Sprite rounded;
        private static Sprite circle;

        public static readonly Color Ink = new(0.075f, 0.03f, 0.025f, 0.97f);
        public static readonly Color Gold = new(0.95f, 0.61f, 0.12f, 1f);
        public static readonly Color Paper = new(0.92f, 0.84f, 0.66f, 0.98f);

        public Canvas CreateCanvas(Transform parent)
        {
            EnsureEventSystem();
            GameObject root = new("Responsive Game HUD", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.55f;
            return canvas;
        }

        public RectTransform Rect(string name, Transform parent)
        {
            GameObject root = new(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        public RectTransform Panel(string name, Transform parent,
            Color color, Vector2 size)
        {
            RectTransform rect = Rect(name, parent);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        public Text Label(string name, string value, Transform parent,
            int size, Color color, Vector2 dimensions, TextAnchor alignment)
        {
            RectTransform rect = Rect(name, parent);
            rect.sizeDelta = dimensions;
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = size;
            return text;
        }

        public Button Button(string name, string value, Transform parent,
            Color color, Vector2 size)
        {
            RectTransform rect = Panel(name, parent, color, size);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(Color.white, Gold, 0.35f);
            colors.pressedColor = Gold;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.72f);
            button.colors = colors;
            Text label = Label("Label", value, rect, 23, Color.white,
                size - new Vector2(12f, 12f), TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            return button;
        }

        public Image Bar(string name, Transform parent, Vector2 size)
        {
            RectTransform background = Rect(name, parent);
            background.sizeDelta = size;
            Image bg = background.gameObject.AddComponent<Image>();
            bg.color = new Color(0.035f, 0.025f, 0.025f, 0.9f);
            RectTransform fill = Rect("Fill", background);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            Image image = fill.gameObject.AddComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            return image;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject root = new("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(root);
        }

        private static Sprite RoundedSprite
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64;
                const float radius = 13f;
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Rounded UI",
                    hideFlags = HideFlags.HideAndDontSave
                };
                Color32[] pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0f) +
                        Mathf.Max(x - (size - radius - 1f), 0f);
                    float dy = Mathf.Max(radius - y, 0f) +
                        Mathf.Max(y - (size - radius - 1f), 0f);
                    byte alpha = dx * dx + dy * dy <= radius * radius
                        ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                rounded = Sprite.Create(texture, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
                return rounded;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circle != null) return circle;
                const int size = 128;
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime UI Circle",
                    hideFlags = HideFlags.HideAndDontSave
                };
                Color32[] pixels = new Color32[size * size];
                float center = (size - 1) * 0.5f;
                float radiusSquared = (center - 1f) * (center - 1f);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    byte alpha = dx * dx + dy * dy <= radiusSquared
                        ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                circle = Sprite.Create(texture, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f);
                return circle;
            }
        }
    }
}
