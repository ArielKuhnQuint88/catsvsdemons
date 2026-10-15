using UnityEngine;

namespace CatsVsDemons.UI
{
    /// <summary>Overlay transitions continue while the game is paused.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class HudReveal : MonoBehaviour
    {
        private CanvasGroup group;
        private float elapsed;

        private void Awake() => group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            Replay();
        }

        public void Replay()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            elapsed = 0f;
            group.alpha = 0f;
            enabled = true;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.24f);
            group.alpha = 1f - Mathf.Pow(1f - t, 3f);
            if (t >= 1f) enabled = false;
        }
    }
}
