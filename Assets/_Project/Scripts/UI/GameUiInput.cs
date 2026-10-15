using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatsVsDemons.UI
{
    internal static class GameUiInput
    {
        private static readonly List<RaycastResult> hits = new();
        private static EventSystem owner;
        private static PointerEventData pointer;
        public static bool IsOverControl(Vector2 position)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return false;
            if (owner != events || pointer == null) { owner = events; pointer = new PointerEventData(events); }
            pointer.position = position; hits.Clear(); events.RaycastAll(pointer, hits);
            return hits.Count > 0;
        }
    }
}
