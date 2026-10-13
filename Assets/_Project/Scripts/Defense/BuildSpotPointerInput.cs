using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CatsVsDemons.Defense
{
    /// <summary>Mouse and touch building also works with the New Input System alone.</summary>
    public sealed class BuildSpotPointerInput : MonoBehaviour
    {
        private static BuildSpotPointerInput instance;
        private readonly List<RaycastResult> uiHits = new();

        public static void Ensure()
        {
            if (instance != null) return;
            instance = new GameObject("Build Spot Pointer Input").AddComponent<BuildSpotPointerInput>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0 || CatsVsDemons.UI.KinGuideOverlay.ConsumedInputThisFrame)
            { BuildSpotHint.SetHovered(null); return; }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) TowerBuildSelection.CancelPlacement();
            Camera camera = Camera.main;
            if (mouse != null && camera != null && TowerBuildSelection.AwaitingPlacement &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
                Physics.Raycast(camera.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hover, 300f, ~0, QueryTriggerInteraction.Ignore))
                BuildSpotHint.SetHovered(hover.collider.GetComponentInParent<BuildSpot>());
            else BuildSpotHint.SetHovered(null);
            if (!TryReadPress(out Vector2 position)) return;
            // This region belongs to Kin's floating joystick.
            if (!TowerBuildSelection.AwaitingPlacement && Application.isMobilePlatform && position.x < Screen.width * 0.42f &&
                position.y < Screen.height * 0.55f) return;
            EventSystem events = EventSystem.current;
            if (events != null)
            {
                uiHits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = position }, uiHits);
                if (uiHits.Count > 0) return;
            }
            if (camera == null) return;
            if (Physics.Raycast(camera.ScreenPointToRay(position), out RaycastHit hit, 300f,
                ~0, QueryTriggerInteraction.Ignore))
                hit.collider.GetComponentInParent<BuildSpot>()?.TryBuild();
        }

        private static bool TryReadPress(out Vector2 position)
        {
            Touchscreen screen = Touchscreen.current;
            if (screen != null)
                foreach (var touch in screen.touches)
                    if (touch.press.wasPressedThisFrame)
                    { position = touch.position.ReadValue(); return true; }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            { position = mouse.position.ReadValue(); return true; }
            position = Vector2.zero; return false;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
