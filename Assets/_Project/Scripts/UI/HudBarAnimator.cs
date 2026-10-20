using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    /// <summary>Smoothed fill, delayed damage trail and a readable low-health warning.</summary>
    public sealed class HudBarAnimator : MonoBehaviour
    {
        private Image fill;
        private Image trail;
        private Color baseColor;
        private float target;
        private float trailDelay;
        private float flash;
        private bool initialized;
        private bool warnAtLowHealth;

        public void Initialize(Image image, bool healthWarning)
        {
            fill = image;
            baseColor = image.color;
            warnAtLowHealth = healthWarning;
            GameObject root = new("Damage Trail", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(image.transform.parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            RectTransform source = image.rectTransform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.offsetMin = source.offsetMin;
            rect.offsetMax = source.offsetMax;
            root.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
            trail = root.GetComponent<Image>();
            trail.sprite = image.sprite;
            trail.type = Image.Type.Filled;
            trail.fillMethod = image.fillMethod;
            trail.fillOrigin = image.fillOrigin;
            trail.color = new Color(1f, 0.77f, 0.47f, 0.85f);
            trail.raycastTarget = false;
            image.raycastTarget = false;
        }

        public void SetValue(float current, float maximum)
        {
            float next = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            if (!initialized)
            {
                initialized = true;
                target = next;
                fill.fillAmount = trail.fillAmount = next;
                return;
            }
            if (Mathf.Approximately(next, target)) return;
            if (next < target)
            {
                trailDelay = 0.32f;
                flash = warnAtLowHealth ? 1f : 0f;
            }
            else
            {
                trail.fillAmount = next;
            }
            target = next;
        }

        private void Update()
        {
            if (!initialized || fill == null) return;
            float dt = Time.unscaledDeltaTime;
            fill.fillAmount = Approach(fill.fillAmount, target, 14f, dt);
            trailDelay -= dt;
            if (trailDelay <= 0f)
                trail.fillAmount = Approach(trail.fillAmount, target, 5f, dt);
            flash = Mathf.Max(0f, flash - dt * 4f);
            float warning = warnAtLowHealth && target > 0f && target <= 0.25f
                ? 0.2f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f : 0f;
            fill.color = Color.Lerp(baseColor, Color.white, Mathf.Max(flash, warning));
        }

        private static float Approach(float value, float goal, float speed, float dt)
        {
            float next = Mathf.Lerp(value, goal, 1f - Mathf.Exp(-speed * dt));
            return Mathf.Abs(next - goal) < 0.001f ? goal : next;
        }
    }
}
