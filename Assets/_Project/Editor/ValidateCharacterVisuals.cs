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
            foreach (string name in new[] { "Kin", "DemonSono", "DemonPoerix", "DemonFlamurk" })
            {
                GameObject holder = new("Rig Check " + name);
                holder.transform.position = new Vector3(1000, 0, 1000);
                bool loaded = RuntimeModelVisuals.Attach(holder.transform, "Models/" + name, 2f, 0, Color.white, false);
                SkinnedMeshRenderer renderer = holder.GetComponentInChildren<SkinnedMeshRenderer>();
                bool valid = loaded && renderer != null && renderer.enabled && renderer.sharedMesh != null;
                if (valid)
                {
                    Mesh mesh = renderer.sharedMesh;
                    int count = renderer.bones.Length;
                    valid = count == mesh.bindposes.Length && count > 0 &&
                        mesh.boneWeights.Length == mesh.vertexCount && renderer.sharedMaterials.Length > 0;
                    foreach (Transform bone in renderer.bones) valid &= bone != null;
                    foreach (BoneWeight weight in mesh.boneWeights)
                        valid &= weight.boneIndex0 >= 0 && weight.boneIndex0 < count &&
                            weight.boneIndex1 >= 0 && weight.boneIndex1 < count &&
                            Mathf.Abs(weight.weight0 + weight.weight1 - 1f) < 0.001f;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        bool shaderValid = material != null && material.shader != null &&
                            material.shader.isSupported && !ShaderUtil.ShaderHasError(material.shader);
                        valid &= shaderValid;
                        if (!shaderValid)
                            Debug.LogError("[Visual] " + name + ": material ausente ou shader com erro. " +
                                "Confira os erros de shader no Console; eles deixam o modelo rosa.");
                    }
                }
                if (valid) { passed++; Debug.Log("[Visual] " + name + ": rig, renderer e materiais válidos."); }
                else Debug.LogError("[Visual] Falha em " + name + ". Verifique a importação do modelo e os shaders.");
                Object.Destroy(holder);
            }
            EditorUtility.DisplayDialog("Verificação de personagens",
                passed + "/4 personagens com rig e materiais válidos. Confira a Game View e os erros do Console.", "OK");
        }
    }
}
