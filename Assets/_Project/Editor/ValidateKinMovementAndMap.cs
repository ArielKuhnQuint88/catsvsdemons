using System.Collections.Generic;
using System.Linq;
using CatsVsDemons.CameraSystem;
using CatsVsDemons.Player;
using CatsVsDemons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.Editor
{
    public static class ValidateKinMovementAndMap
    {
        [MenuItem("Tools/Cats vs Demons/Verificar movimento câmera e minimapa (Play Mode)")]
        public static void Validate()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Kin e minimapa", "Entre em Play Mode primeiro.", "OK"); return; }
            List<string> errors = new();
            KinPrototypeController kin = Object.FindFirstObjectByType<KinPrototypeController>();
            CameraModeController camera = Camera.main != null ? Camera.main.GetComponent<CameraModeController>() : null;
            BattleMinimap map = Object.FindFirstObjectByType<BattleMinimap>();
            if (kin == null) errors.Add("Kin não está ativo no jardim.");
            else
            {
                KinCombatVisual hand = kin.GetComponent<KinCombatVisual>();
                if (hand == null || !hand.HasGardenHand) errors.Add("Pata e katana do jardim não foram criadas.");
                if (camera != null && camera.IsFirstPerson && (hand == null || !hand.HasFirstPersonHand)) errors.Add("Pata da primeira pessoa ausente.");
                Transform body = kin.transform.Find("GameplayModel");
                if (body != null && camera != null && !camera.IsFirstPerson)
                {
                    Renderer[] rendered = body.GetComponentsInChildren<Renderer>(true);
                    if (!rendered.Any(r => r.enabled && !r.forceRenderingOff)) errors.Add("Corpo continua oculto na visão isométrica.");
                }
            }
            if (camera == null) errors.Add("Controlador de câmera principal ausente.");
            if (map == null) errors.Add("Minimapa ativo ausente.");
            Button switchView = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Switch Camera View");
            if (switchView == null) errors.Add("Botão de troca de visão ausente.");
            string message = errors.Count > 0 ? string.Join("\n", errors) :
                "Pata/katana, câmera e minimapa encontrados. Demônios: " + map.VisibleDemons + "; torres: " + map.VisibleTowers +
                "; alertas: " + map.TowersUnderAttack + ". Teste as oito direções, inclusive S/↓ e diagonais perto de demônios: Kin deve virar para a direção escolhida. Confira ataque em movimento, V, ambas as câmeras, dano numa torre e joystick + olhar no Android. Confira a animação e a física na Game View.";
            if (errors.Count == 0) Debug.Log("[Kin e minimapa] " + message); else Debug.LogError("[Kin e minimapa] " + message);
            EditorUtility.DisplayDialog("Movimento, câmera e minimapa", message, "OK");
        }
    }
}
