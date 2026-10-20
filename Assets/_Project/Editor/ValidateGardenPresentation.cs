using System.Collections.Generic;
using System.Linq;
using CatsVsDemons.Visuals;
using UnityEditor;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class ValidateGardenPresentation
    {
        [MenuItem("Tools/Cats vs Demons/Verificar cenário e caminhos (Play Mode)")]
        public static void Validate()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Cenário", "Entre em Play Mode primeiro.", "OK");
                return;
            }
            List<string> errors = new();
            Transform paths = GameObject.Find("Game/Paths")?.transform;
            int routes = 0;
            List<Vector3[]> worldRoutes = new();
            if (paths == null) errors.Add("Raiz dos caminhos ausente.");
            else foreach (Transform path in paths)
            {
                Transform[] joints = path.Cast<Transform>().Where(t => t.name.StartsWith("Joint_")).
                    OrderBy(t => t.name, System.StringComparer.Ordinal).ToArray();
                if (joints.Length == 0) continue;
                routes++;
                worldRoutes.Add(joints.Select(j => j.position).ToArray());
                Transform surface = path.Find(path.name + "_Surface");
                Mesh mesh = surface != null ? surface.GetComponent<MeshFilter>()?.sharedMesh : null;
                if (mesh == null || mesh.vertexCount != joints.Length * 2)
                    errors.Add(path.name + ": malha e pontos de navegação não correspondem.");
                else
                {
                    if (mesh.normals.Any(n => n.y < 0.98f)) errors.Add(path.name + ": faces do piso invertidas.");
                    MeshCollider collider = surface.GetComponent<MeshCollider>();
                    if (collider == null || collider.sharedMesh != mesh)
                        errors.Add(path.name + ": collider não acompanha a malha do piso.");
                }
                foreach (Transform segment in path)
                    if (segment.name.StartsWith("Segment_") && segment.GetComponent<Renderer>()?.enabled == true)
                        errors.Add(path.name + ": trecho antigo ainda visível.");
            }
            if (routes < 3) errors.Add("A campanha precisa ter pelo menos três rotas.");
            GardenArtDirector art = Object.FindFirstObjectByType<GardenArtDirector>();
            if (art == null) errors.Add("Direção de arte ausente.");
            else foreach (Vector3 pond in art.PondPositions)
                if (GardenPathGeometry.DistanceToRoutes(pond, worldRoutes) + 0.001f < art.PondRouteClearance)
                    errors.Add("Lago sem folga para a margem, ponte e borda do caminho: " + pond);
            GameObject garden = GameObject.Find("Game/Art Directed Japanese Garden");
            int triangles = 0;
            if (garden == null) errors.Add("Novo cenário não foi criado.");
            else foreach (MeshFilter filter in garden.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null) triangles += filter.sharedMesh.triangles.Length / 3;
            foreach (string name in new[] { "CatsVsDemons/GardenScenery", "CatsVsDemons/GardenWater", "Skybox/Procedural" })
            {
                Shader shader = Shader.Find(name);
                if (shader == null || ShaderUtil.ShaderHasError(shader)) errors.Add("Shader ausente/com erro: " + name);
            }
            string message = errors.Count == 0
                ? routes + " rotas contínuas com navegação e colliders sincronizados. " + triangles +
                    " triângulos no cenário agrupado. Lagos com margens e pontes fora dos caminhos. Confira a aparência e o FPS no jogo."
                : string.Join("\n", errors);
            if (errors.Count == 0) Debug.Log("[Cenário] " + message);
            else Debug.LogError("[Cenário] " + message);
            EditorUtility.DisplayDialog("Cenário e caminhos", message, "OK");
        }
    }
}
