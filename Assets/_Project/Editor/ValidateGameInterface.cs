using System.Collections.Generic;
using System.Linq;
using CatsVsDemons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatsVsDemons.Editor
{
    public static class ValidateGameInterface
    {
        [MenuItem("Tools/Cats vs Demons/Verificar interface (Play Mode)")]
        public static void Validate()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Interface", "Entre em Play Mode primeiro.", "OK"); return;
            }
            Canvas.ForceUpdateCanvases();
            List<string> errors = new();
            if (Object.FindFirstObjectByType<EventSystem>() == null) errors.Add("EventSystem ausente.");
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases.Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay))
            {
                if (canvas.GetComponent<CanvasScaler>() == null) errors.Add(canvas.name + ": escala responsiva ausente.");
                if (canvas.GetComponent<GraphicRaycaster>() == null) errors.Add(canvas.name + ": entrada de mouse/toque ausente.");
                foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
                    if (text.raycastTarget) errors.Add(text.name + ": texto intercepta o toque.");
                foreach (Button button in canvas.GetComponentsInChildren<Button>().Where(b => b.IsActive()))
                {
                    if (button.targetGraphic == null) errors.Add(button.name + ": alvo de interação ausente.");
                    if (button.GetComponent<HudMotion>() == null) errors.Add(button.name + ": animação de interação ausente.");
                    RectTransform rect = (RectTransform)button.transform;
                    Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                        if (!Screen.safeArea.Contains(point)) { errors.Add(button.name + ": botão fora da área segura."); break; }
                    }
                }
            }
            ResponsiveCanvasHud hud = Object.FindFirstObjectByType<ResponsiveCanvasHud>();
            if (hud != null)
            {
                Transform[] children = hud.GetComponentsInChildren<Transform>(true);
                if (children.Any(t => t.name == "Construction Step" || t.name == "Defense Description" || t.name == "Defense Dock"))
                    errors.Add("O seletor antigo de torres ainda está na cena.");
                if (children.Count(t => t.name.StartsWith("Defense ") && t.GetComponent<Button>() != null) != 3)
                    errors.Add("As três defesas precisam estar disponíveis.");
                if (!children.Any(t => t.name == "Switch Camera View" && t.GetComponent<Button>() != null)) errors.Add("Botão de câmera junto do minimapa ausente.");
                if (!children.Any(t => t.name == "Map Roads") || !children.Any(t => t.name == "Map Actors")) errors.Add("Camadas de caminhos/personagens do minimapa ausentes.");
            }
            if (Object.FindFirstObjectByType<KinGuideOverlay>() == null) errors.Add("Guia permanente do Kin ausente.");
            string result = errors.Count == 0
                ? "Interface responsiva, três defesas sem cartões antigos, textos sem bloqueio de toque e guia disponíveis. Confira leitura, seleção, animações, joystick, compras e pausa na Game View e no Android."
                : string.Join("\n", errors.Distinct());
            if (errors.Count == 0) Debug.Log("[Interface] " + result); else Debug.LogError("[Interface] " + result);
            EditorUtility.DisplayDialog("Interface do jogo", result, "OK");
        }
    }
}
