using System.Collections.Generic;
using CatsVsDemons.Player;
using UnityEditor;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class ValidateKinEquipment
    {
        [MenuItem("Tools/Cats vs Demons/Verificar roupas e acessórios (Play Mode)")]
        public static void Validate()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Equipamento do Kin", "Entre em Play Mode primeiro.", "OK");
                return;
            }
            KinEquipmentView equipment = Object.FindFirstObjectByType<KinEquipmentView>();
            if (equipment == null || !equipment.HasEquipment)
            {
                Debug.LogError("[Equipamento] Kin não recebeu as peças equipadas. Confira o carregamento da loja.");
                return;
            }
            List<string> errors = new();
            Transform model = equipment.transform.Find("GameplayModel");
            Transform wardrobe = null;
            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
                if (node.name == "Equipped Kin Wardrobe" && node.gameObject.activeSelf) wardrobe = node;
            int triangles = 0, parts = 0;
            if (wardrobe == null) errors.Add("Raiz do equipamento ausente do modelo animado.");
            else
            {
                foreach (MeshFilter filter in wardrobe.GetComponentsInChildren<MeshFilter>(true))
                {
                    parts++;
                    Mesh mesh = filter.sharedMesh;
                    if (mesh == null || mesh.vertexCount == 0) { errors.Add("Peça sem geometria: " + filter.name); continue; }
                    triangles += mesh.triangles.Length / 3;
                    if (mesh.colors.Length != mesh.vertexCount || mesh.uv.Length != mesh.vertexCount)
                        errors.Add("Cores ou UVs incompletas: " + filter.name);
                    MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                    Material material = renderer != null ? renderer.sharedMaterial : null;
                    if (material == null || material.shader == null || ShaderUtil.ShaderHasError(material.shader))
                        errors.Add("Material/shader com falha: " + filter.name);
                    else if (filter.name.Contains("transparent blue lenses"))
                    {
                        if (material.GetFloat("_Surface") < 0.5f || material.GetFloat("_ZWrite") > 0.5f)
                            errors.Add("Lentes configuradas como superfícies opacas.");
                    }
                    else if (!material.HasProperty("_UseVertexColor") || material.GetFloat("_UseVertexColor") < 0.5f)
                        errors.Add("Peça sem paleta de tecido e ornamentos: " + filter.name);
                }
                if (wardrobe.GetComponentsInChildren<Collider>(true).Length > 0)
                    errors.Add("Equipamento não deve criar colliders adicionais.");
            }
            string message = errors.Count == 0
                ? equipment.OutfitId + " + " + (string.IsNullOrEmpty(equipment.AccessoryId) ? "sem acessório" : equipment.AccessoryId) +
                    ": " + parts + " peças, " + triangles + " triângulos. Confira encaixe, caminhada, ataque, loja e câmera na Game View."
                : string.Join("\n", errors);
            if (errors.Count == 0) Debug.Log("[Equipamento] " + message); else Debug.LogError("[Equipamento] " + message);
            EditorUtility.DisplayDialog("Roupas e acessórios", message, "OK");
        }
    }
}
