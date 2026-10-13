using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    public enum TutorialFocus { None, Movement, LanternButton, BuildSpot, LanternPower, Enemy, SpecialButton }

    /// <summary>Kin's dialogue and non-blocking guidance share the HUD safe area.</summary>
    public sealed class KinTutorialPresenter : MonoBehaviour
    {
        private RectTransform safeArea;
        private RectTransform card;
        private RectTransform portrait;
        private RectTransform highlight;
        private RectTransform marker;
        private RectTransform control;
        private Transform worldTarget;
        private Text title;
        private Text body;
        private Text action;
        private Text markerLabel;
        private Text progress;
        private Image halo;
        private Image[] border;
        private Sprite portraitSprite;
        private Button skip;
        private CanvasGroup cardGroup;
        private CanvasGroup cuesGroup;
        private string sentence = "";
        private float letters;
        private float reveal;
        private bool shown;
        private bool suspended;
        private TutorialFocus focus;
        private Action skipAction;
        private Camera camera;
        private int width;
        private int height;
        private readonly Vector3[] controlCorners = new Vector3[4];
        private Text targetArrow;
        private string targetCaption;

        internal void Initialize(RuntimeUiFactory ui, RectTransform parent)
        {
            safeArea = parent;
            RectTransform root = ui.Rect("Kin Tutorial", parent);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            card = ui.Panel("Speech Bubble", root, RuntimeUiFactory.Paper, new Vector2(850, 228));
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0);
            card.pivot = new Vector2(0.5f, 0);
            card.anchoredPosition = new Vector2(0, 28);
            card.GetComponent<Image>().raycastTarget = false;
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.17f, 0.065f, 0.025f, 0.9f);
            outline.effectDistance = new Vector2(3, -3);
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            portrait = ui.Rect("Kin Speaking", card);
            portrait.sizeDelta = new Vector2(164, 164);
            portrait.anchoredPosition = new Vector2(-326, 4);
            Image avatar = portrait.gameObject.AddComponent<Image>();
            Texture2D texture = Resources.Load<Texture2D>("UI/KinTutorialPortrait");
            if (texture != null)
            {
                portraitSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                avatar.sprite = portraitSprite;
            }
            avatar.preserveAspect = true; avatar.raycastTarget = false;
            Text name = ui.Label("Speaker", "KIN", card, 20, RuntimeUiFactory.Ink,
                new Vector2(160, 26), TextAnchor.MiddleCenter);
            ((RectTransform)name.transform).anchoredPosition = new Vector2(-326, -80);
            title = ui.Label("Lesson", "", card, 24, new Color(0.54f, 0.12f, 0.04f),
                new Vector2(610, 34), TextAnchor.MiddleLeft);
            ((RectTransform)title.transform).anchoredPosition = new Vector2(90, 68);
            body = ui.Label("Kin Dialogue", "", card, 23, RuntimeUiFactory.Ink,
                new Vector2(610, 64), TextAnchor.UpperLeft);
            ((RectTransform)body.transform).anchoredPosition = new Vector2(90, 16);
            action = ui.Label("Your Objective", "", card, 20, new Color(0.12f, 0.32f, 0.22f),
                new Vector2(610, 42), TextAnchor.MiddleLeft);
            ((RectTransform)action.transform).anchoredPosition = new Vector2(90, -39);
            progress = ui.Label("Objective Progress", "", card, 17, RuntimeUiFactory.Ink,
                new Vector2(390, 28), TextAnchor.MiddleLeft);
            ((RectTransform)progress.transform).anchoredPosition = new Vector2(-20, -82);
            skip = ui.Button("Skip Tutorial", "PULAR", card, new Color(0.36f, 0.2f, 0.08f), new Vector2(158, 62));
            ((RectTransform)skip.transform).anchoredPosition = new Vector2(315, -80);
            skip.onClick.AddListener(() => skipAction?.Invoke());

            RectTransform cues = ui.Rect("Tutorial Alerts", root);
            cues.anchorMin = Vector2.zero; cues.anchorMax = Vector2.one;
            cues.offsetMin = cues.offsetMax = Vector2.zero;
            cuesGroup = cues.gameObject.AddComponent<CanvasGroup>();
            cuesGroup.blocksRaycasts = false; cuesGroup.interactable = false;
            highlight = ui.Rect("Control Highlight", cues);
            border = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                RectTransform edge = ui.Panel("Edge " + i, highlight, RuntimeUiFactory.Gold, Vector2.zero);
                edge.anchorMin = i < 2 ? new Vector2(0, i) : new Vector2(i - 2, 0);
                edge.anchorMax = i < 2 ? new Vector2(1, i) : new Vector2(i - 2, 1);
                edge.sizeDelta = i < 2 ? new Vector2(0, 5) : new Vector2(5, 0);
                edge.anchoredPosition = Vector2.zero;
                border[i] = edge.GetComponent<Image>(); border[i].raycastTarget = false;
            }
            marker = ui.Rect("Action Marker", cues);
            marker.sizeDelta = new Vector2(112, 112);
            halo = marker.gameObject.AddComponent<Image>();
            halo.sprite = RuntimeUiFactory.RingSprite; halo.color = RuntimeUiFactory.Gold;
            halo.raycastTarget = false;
            Text arrow = ui.Label("Arrow", "▼", marker, 38, RuntimeUiFactory.Gold,
                new Vector2(60, 60), TextAnchor.MiddleCenter);
            targetArrow = arrow;
            ((RectTransform)arrow.transform).anchoredPosition = new Vector2(0, 90);
            RectTransform tag = ui.Panel("Action Label", marker, RuntimeUiFactory.Paper, new Vector2(286, 40));
            tag.anchoredPosition = new Vector2(0, 138); tag.GetComponent<Image>().raycastTarget = false;
            markerLabel = ui.Label("Target Name", "", tag, 19, RuntimeUiFactory.Ink,
                new Vector2(276, 32), TextAnchor.MiddleCenter);
            marker.gameObject.SetActive(false); highlight.gameObject.SetActive(false);
            card.gameObject.SetActive(false);
        }

        public void Show(string lesson, string dialogue, string objective, Action onSkip)
        {
            bool changed = title.text != lesson;
            shown = true; skipAction = onSkip;
            title.text = lesson; action.text = objective;
            card.gameObject.SetActive(!suspended);
            if (changed)
            {
                sentence = dialogue; letters = 0; body.text = ""; reveal = 0;
                progress.text = ""; cardGroup.alpha = 0;
            }
        }

        public void SetFocus(TutorialFocus kind, RectTransform button, Transform target, string caption)
        {
            focus = kind; control = button; worldTarget = target; targetCaption = caption;
            markerLabel.text = caption;
        }

        public void SetProgress(string text)
        {
            if (progress.text != text) progress.text = text;
        }

        public void SetSuspended(bool value)
        {
            suspended = value;
            card.gameObject.SetActive(shown && !value);
            cuesGroup.alpha = shown && !value ? 1 : 0;
        }

        public void Hide()
        {
            shown = false; card.gameObject.SetActive(false);
            marker.gameObject.SetActive(false); highlight.gameObject.SetActive(false);
            worldTarget = null; control = null;
        }

        private void Update()
        {
            if (!shown || suspended) return;
            if (width != Screen.width || height != Screen.height)
            {
                width = Screen.width; height = Screen.height;
                // Leave the joystick and special button unobstructed on narrow displays.
                float available = Mathf.Max(600, safeArea.rect.width - 420);
                card.localScale = Vector3.one * Mathf.Min(1f, available / 850f);
            }
            reveal += Time.unscaledDeltaTime;
            cardGroup.alpha = Mathf.Clamp01(reveal / 0.22f);
            int previous = Mathf.FloorToInt(letters);
            letters = Mathf.Min(sentence.Length, letters + Time.unscaledDeltaTime * 42f);
            if (previous != Mathf.FloorToInt(letters)) body.text = sentence.Substring(0, Mathf.FloorToInt(letters));
            bool talking = letters < sentence.Length;
            portrait.localScale = Vector3.one * (talking ? 1f + Mathf.Sin(Time.unscaledTime * 11f) * 0.025f : 1f);
            portrait.localRotation = Quaternion.Euler(0, 0, talking ? Mathf.Sin(Time.unscaledTime * 5f) * 2f : 0f);
            float pulse = 0.6f + Mathf.Sin(Time.unscaledTime * 5f) * 0.25f;
            foreach (Image edge in border) edge.color = new Color(1f, 0.7f, 0.15f, pulse);
            highlight.gameObject.SetActive(control != null);
            if (control != null)
            {
                control.GetWorldCorners(controlCorners);
                Vector2 min = safeArea.InverseTransformPoint(controlCorners[0]);
                Vector2 max = safeArea.InverseTransformPoint(controlCorners[2]);
                highlight.anchoredPosition = (min + max) * 0.5f;
                highlight.sizeDelta = max - min + new Vector2(18, 18);
            }
            camera = Camera.main;
            bool world = worldTarget != null && camera != null && focus != TutorialFocus.None;
            marker.gameObject.SetActive(world);
            if (!world) return;
            Vector3 screen = camera.WorldToScreenPoint(worldTarget.position + Vector3.up * 0.6f);
            if (screen.z <= 0) { marker.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safeArea, screen, null, out Vector2 point);
            Rect rect = safeArea.rect;
            Vector2 original = point;
            point.x = Mathf.Clamp(point.x, rect.xMin + 154, rect.xMax - 154);
            point.y = Mathf.Clamp(point.y, rect.yMin + 320, Mathf.Max(rect.yMin + 320, rect.yMax - 500));
            Vector2 direction = original - point;
            targetArrow.transform.localRotation = direction.sqrMagnitude > 1f
                ? Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f)
                : Quaternion.identity;
            string caption = direction.sqrMagnitude > 1f ? "SIGA A SETA" : targetCaption;
            if (markerLabel.text != caption) markerLabel.text = caption;
            marker.anchoredPosition = point;
            halo.color = new Color(1f, 0.72f, 0.16f, pulse);
            marker.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f);
        }

        private void OnDestroy()
        {
            if (portraitSprite != null) Destroy(portraitSprite);
        }
    }
}
