using CatsVsDemons.Visuals;
using UnityEditor;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class ValidateCharacterVisuals
    {
        [MenuItem("Tools/Cats vs Demons/Verificar personagens (Play Mode)")]
        public static void Validate()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Verificação de personagens", "Entre em Play Mode primeiro.", "OK");
                return;
            }
            int passed = 0;
            int usingFallback = 0;
            foreach (string name in new[] { "Kin", "DemonSono", "DemonPoerix", "DemonFlamurk" })
            {
                GameObject holder = new("Rig Check " + name);
                holder.transform.position = new Vector3(1000, 0, 1000);
                bool loaded = RuntimeModelVisuals.Attach(holder.transform, "Models/" + name, 2f, 0, Color.white, false);
                // Verify the final equipment palette, not only the material before the shop changes it.
                if (loaded && name == "Kin")
                    holder.AddComponent<CatsVsDemons.Player.KinShopLoadout>().Apply("samurai_vermelho", "");
                SkinnedMeshRenderer renderer = holder.GetComponentInChildren<SkinnedMeshRenderer>();
                MeshFilter source = holder.GetComponentInChildren<MeshFilter>();
                MeshRenderer original = source != null ? source.GetComponent<MeshRenderer>() : null;
                bool originalValid = loaded && original != null && original.enabled && source.sharedMesh != null &&
                    source.sharedMesh.vertexCount > 0 && MaterialsValid(original, name);
                bool rigValid = loaded && renderer != null && renderer.enabled && renderer.sharedMesh != null;
                if (rigValid)
                {
                    Mesh mesh = renderer.sharedMesh;
                    int count = renderer.bones.Length;
                    rigValid = count == mesh.bindposes.Length && count > 0 &&
                        mesh.boneWeights.Length == mesh.vertexCount && renderer.sharedMaterials.Length > 0;
                    foreach (Transform bone in renderer.bones) rigValid &= bone != null;
                    foreach (BoneWeight weight in mesh.boneWeights)
                        rigValid &= weight.boneIndex0 >= 0 && weight.boneIndex0 < count &&
                            weight.boneIndex1 >= 0 && weight.boneIndex1 < count &&
                            Mathf.Abs(weight.weight0 + weight.weight1 - 1f) < 0.001f;
                    rigValid &= renderer.GetComponent<MeshRenderer>() == null &&
                        renderer.rootBone == renderer.transform.parent && MaterialsValid(renderer, name);
                    rigValid &= source != null && AutomaticCharacterRig.ValidateRestPose(renderer, source.sharedMesh, out _);
                }
                bool valid = originalValid || rigValid;
                if (valid && name == "Kin")
                {
                    Mesh mesh = rigValid ? renderer.sharedMesh : source.sharedMesh;
                    Renderer surface = rigValid ? renderer : original;
                    bool textured = mesh.uv.Length == mesh.vertexCount;
                    foreach (Material material in surface.sharedMaterials)
                        textured &= material != null && material.HasProperty("_DetailMap") && material.GetTexture("_DetailMap") != null &&
                            material.GetFloat("_DetailStrength") > 0f;
                    if (!textured)
                    {
                        Debug.LogError("[Visual] Kin: UVs ou textura de detalhe ausentes.");
                        valid = false;
                    }
                    if (mesh.subMeshCount < 4)
                        Debug.LogWarning("[Visual] Kin: paleta reduzida na importação. Reimporte Resources/Models/Kin.obj para recuperar quatro superfícies.");
                    else if (!KinPaletteValid(surface)) valid = false;
                }
                if (valid)
                {
                    passed++;
                    if (!rigValid) usingFallback++;
                    Debug.Log("[Visual] " + name + ": modelo carregado; rig " + (rigValid ? "válido" : "em fallback") +
                        "; malha original " + (originalValid ? "disponível" : "indisponível") + ".");
                }
                else Debug.LogError("[Visual] Falha em " + name + ". Verifique a importação do modelo e os shaders.");
                Object.Destroy(holder);
            }
            EditorUtility.DisplayDialog("Verificação de personagens",
                passed + "/4 modelos disponíveis; " + usingFallback + " em animação simples. " +
                "A geometria do rig foi conferida quando disponível. Confira a Game View e o Console.", "OK");
        }

        private static bool MaterialsValid(Renderer renderer, string name)
        {
            if (renderer.sharedMaterials.Length == 0) return false;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.shader != null && material.shader.isSupported &&
                    !ShaderUtil.ShaderHasError(material.shader)) continue;
                Debug.LogError("[Visual] " + name + ": material ausente ou shader com erro. Confira o Console.");
                return false;
            }
            return true;
        }

        private static bool KinPaletteValid(Renderer renderer)
        {
            bool furFound = false;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || !KinSurfacePalette.IsKnownPart(material.name) ||
                    !material.HasProperty("_UseHeightPalette") || material.GetFloat("_UseHeightPalette") > 0.001f)
                {
                    Debug.LogError("[Visual] Kin: superfície sem identidade ou ainda usando faixas de cor por altura.");
                    return false;
                }
                Color color = material.GetColor("_BaseColor");
                Color accent = material.GetColor("_AccentColor");
                if (Mathf.Abs(color.r - accent.r) + Mathf.Abs(color.g - accent.g) + Mathf.Abs(color.b - accent.b) > 0.01f)
                {
                    Debug.LogError("[Visual] Kin: equipamento recolorindo a superfície com outra cor de destaque.");
                    return false;
                }
                if (!material.name.ToLowerInvariant().Contains("ded1cf")) continue;
                furFound = true;
                if (color.r + color.g + color.b < 2.7f)
                {
                    Debug.LogError("[Visual] Kin: a pelagem perdeu a cor clara após aplicar a roupa.");
                    return false;
                }
            }
            if (!furFound) Debug.LogError("[Visual] Kin: material da pelagem não encontrado.");
            return furFound;
        }

        [MenuItem("Tools/Cats vs Demons/Restaurar modelos originais (Play Mode)")]
        public static void RestoreOriginalModels()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Restaurar personagens", "Entre em Play Mode primeiro.", "OK");
                return;
            }
            int count = 0;
            AutomaticCharacterRig.EnableSimpleAnimationForSession();
            foreach (AutomaticCharacterRig rig in Object.FindObjectsByType<AutomaticCharacterRig>(FindObjectsSortMode.None))
            {
                // Kin is deliberately hidden inside the first-person camera.
                if (PlayerPrefs.GetInt("CameraMode", 0) == 1 && !Application.isMobilePlatform &&
                    rig.GetComponentInParent<CatsVsDemons.Player.KinHealth>() != null) continue;
                rig.UseOriginalRenderer();
                count++;
            }
            Debug.Log("[Visual] " + count + " modelos restaurados. Novos inimigos também usarão animação simples até sair do Play Mode.");
        }
    }
}
