using UnityEngine;

namespace CatsVsDemons.UI
{
    /// <summary>Fits a fixed-layout menu to the real parent safe area without scaling individual hit targets.</summary>
    internal sealed class UiSafeFit : MonoBehaviour
    {
        public Vector2 Padding = new(64, 64);
        private Vector2 lastSize;
        private void OnEnable() => lastSize = Vector2.zero;
        private void LateUpdate()
        {
            RectTransform rect = transform as RectTransform;
            RectTransform parent = transform.parent as RectTransform;
            if (rect == null || parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            Vector2 size = parent.rect.size;
            if (size == lastSize) return;
            lastSize = size;
            float scale = Mathf.Min(1f, Mathf.Min((size.x - Padding.x) / Mathf.Max(1, rect.sizeDelta.x),
                (size.y - Padding.y) / Mathf.Max(1, rect.sizeDelta.y)));
            rect.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }
    }
}
