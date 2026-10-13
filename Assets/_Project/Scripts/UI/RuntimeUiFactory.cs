using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    internal sealed class RuntimeUiFactory
    {
        private readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static readonly Color Ink = new(.075f, .16f, .15f, 1f);
        public static readonly Color Jade = new(.16f, .34f, .29f, 1f);
        public static readonly Color Gold = new(.85f, .72f, .45f, 1f);
        public static readonly Color Paper = new(.97f, .94f, .85f, .98f);
        public static readonly Color Muted = new(.67f, .76f, .67f, 1f);
        public static readonly Color Coral = new(.72f, .32f, .26f, 1f);
        public static readonly Color Veil = new(.035f, .08f, .075f, .9f);
        public static Sprite CircleSprite => GameUiArt.Circle;
        public static Sprite RingSprite => GameUiArt.Ring;

        public Canvas CreateCanvas(Transform parent)
        {
            EnsureEventSystem();
            GameObject root = new("Responsive Game HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .55f;
            return canvas;
        }

        public RectTransform Rect(string name, Transform parent)
        {
            GameObject root = new(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            return rect;
        }

        public RectTransform Panel(string name, Transform parent, Color color, Vector2 size)
        {
            RectTransform rect = Rect(name, parent); rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GameUiArt.Rounded; image.type = Image.Type.Sliced; image.color = color;
            return rect;
        }

        public RectTransform Surface(string name, Transform parent, Color color, Vector2 size, bool ornament = false)
        {
            RectTransform rect = Panel(name, parent, color, size);
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.015f, .04f, .035f, .32f); shadow.effectDistance = new Vector2(0, -6);
            Trim(rect, Gold, ornament);
            return rect;
        }

        public void Trim(RectTransform rect, Color color, bool ornament = false)
        {
            RectTransform border = Rect("Fine Gold Contour", rect);
            Stretch(border, new Vector2(3, 3));
            Image contour = border.gameObject.AddComponent<Image>();
            contour.sprite = GameUiArt.Contour; contour.type = Image.Type.Sliced;
            contour.color = new Color(color.r, color.g, color.b, .64f); contour.raycastTarget = false;
            if (!ornament) return;
            for (int side = -1; side <= 1; side += 2)
            {
                RectTransform seal = Rect("Garden Seal", rect); seal.sizeDelta = new Vector2(22, 22);
                seal.anchorMin = seal.anchorMax = new Vector2(side < 0 ? 0 : 1, 1);
                seal.anchoredPosition = new Vector2(side * -24, -24); seal.localRotation = Quaternion.Euler(0, 0, 45);
                Image image = seal.gameObject.AddComponent<Image>(); image.sprite = GameUiArt.Contour;
                image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            }
        }

        public Text Label(string name, string value, Transform parent, int size, Color color, Vector2 dimensions, TextAnchor alignment)
        {
            RectTransform rect = Rect(name, parent); rect.sizeDelta = dimensions;
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value; text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.color = color; text.alignment = alignment; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(16, size); text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        public Button Button(string name, string value, Transform parent, Color color, Vector2 size)
        {
            RectTransform rect = Surface(name, parent, color, size);
            rect.GetComponent<Image>().sprite = GameUiArt.Button;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); ConfigureButton(button);
            Label("Label", value, rect, 23, TextColor(color), size - new Vector2(32, 14), TextAnchor.MiddleCenter);
            rect.gameObject.AddComponent<HudMotion>();
            return button;
        }

        public Button ClearButton(string name, Transform parent, Vector2 size)
        {
            RectTransform rect = Rect(name, parent); rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); ConfigureButton(button);
            button.targetGraphic = image; rect.gameObject.AddComponent<HudMotion>();
            return button;
        }

        public static void ConfigureButton(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.08f, 1.08f, 1.03f, 1);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.78f, .85f, .8f, 1);
            colors.disabledColor = new Color(.75f, .82f, .76f, 1f); colors.fadeDuration = .12f;
            button.colors = colors;
        }

        public static Color TextColor(Color background) => background.r * .299f + background.g * .587f + background.b * .114f > .6f ? Ink : Paper;

        public Image Icon(string name, UiIcon symbol, Transform parent, Vector2 size, Color color)
        {
            RectTransform rect = Rect(name, parent); rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = GameUiArt.Symbol(symbol);
            image.color = color; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        public Image Disc(string name, Transform parent, Vector2 size, Color color, bool ring = false)
        {
            RectTransform rect = Rect(name, parent); rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = ring ? RingSprite : CircleSprite;
            image.color = color; image.raycastTarget = false;
            return image;
        }

        public void Rule(Transform parent, float width, Vector2 position)
        {
            RectTransform line = Panel("Gold Hairline", parent, new Color(Gold.r, Gold.g, Gold.b, .4f), new Vector2(width, 2));
            line.anchoredPosition = position; line.GetComponent<Image>().raycastTarget = false;
        }

        public Slider Slider(string name, Transform parent, Vector2 size, float initial)
        {
            RectTransform root = Rect(name, parent); root.sizeDelta = size;
            // A full-height transparent hit area allows tapping or dragging anywhere on the track.
            root.gameObject.AddComponent<Image>().color = Color.clear;
            Slider slider = root.gameObject.AddComponent<Slider>(); ConfigureButton(slider);
            RectTransform track = Panel("Track", root, new Color(.24f, .38f, .31f), new Vector2(size.x, 8));
            track.GetComponent<Image>().raycastTarget = false;
            RectTransform fillArea = Rect("Fill Area", root); Stretch(fillArea, new Vector2(13, 0));
            RectTransform fill = Panel("Volume Fill", fillArea, Gold, Vector2.zero); Stretch(fill, Vector2.zero);
            fill.GetComponent<Image>().raycastTarget = false;
            fill.offsetMin = new Vector2(0, (size.y - 8) * .5f); fill.offsetMax = new Vector2(0, -(size.y - 8) * .5f);
            RectTransform handleArea = Rect("Handle Area", root); Stretch(handleArea, new Vector2(13, 0));
            Image handle = Disc("Volume Handle", handleArea, new Vector2(30, 30), Paper);
            handle.raycastTarget = true; slider.fillRect = fill; slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = handle; slider.minValue = 0; slider.maxValue = 1; slider.value = initial;
            return slider;
        }

        private static void ConfigureButton(Selectable selectable)
        {
            ColorBlock colors = selectable.colors; colors.highlightedColor = Color.white;
            colors.pressedColor = Gold; colors.disabledColor = new Color(1, 1, 1, .4f); selectable.colors = colors;
        }

        public Image Bar(string name, Transform parent, Vector2 size)
        {
            RectTransform background = Panel(name, parent, new Color(.045f, .11f, .09f, .88f), size);
            background.GetComponent<Image>().raycastTarget = false;
            RectTransform fill = Rect("Fill", background); Stretch(fill, new Vector2(2, 2));
            Image image = fill.gameObject.AddComponent<Image>(); image.sprite = GameUiArt.Rounded;
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left; image.raycastTarget = false;
            return image;
        }

        public static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = inset; rect.offsetMax = -inset;
        }

        public static void Fit(RectTransform rect, Vector2 padding)
        {
            UiSafeFit fit = rect.gameObject.AddComponent<UiSafeFit>(); fit.Padding = padding;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject root = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); Object.DontDestroyOnLoad(root);
        }
    }
}
