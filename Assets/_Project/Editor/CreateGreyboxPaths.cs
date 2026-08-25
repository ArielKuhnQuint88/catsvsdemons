using CatsVsDemons.Defense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateGreyboxPaths
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const float PathWidth = 2.45f;
        private const float PathHeight = 0.12f;
        private const float BorderWidth = 2.85f;
        private const int CurveSubdivisions = 8;
        private const string PathMeshFolder =
            ProjectRoot + "/Art/Meshes/Paths";

        [MenuItem("Tools/Cats vs Demons/Add Serpentine Paths")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            Transform pathsRoot = FindRequired("Game/Paths");
            Transform buildSpotsRoot = FindRequired("Game/BuildSpots");
            Transform enemiesRoot = FindRequired("Game/Enemies");

            if (pathsRoot == null || buildSpotsRoot == null || enemiesRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "A cena Game não possui a Hierarchy esperada. Execute primeiro Create Greybox Scene.",
                    "OK"
                );
                return;
            }

            if ((pathsRoot.childCount > 0 || buildSpotsRoot.childCount > 0) &&
                !EditorUtility.DisplayDialog(
                    "Recriar caminhos?",
                    "Os caminhos e pontos de construção existentes serão substituídos.",
                    "Recriar",
                    "Cancelar"))
            {
                return;
            }

            ClearChildren(pathsRoot);
            ClearChildren(buildSpotsRoot);

            Transform spawnRoot = enemiesRoot.Find("SpawnPoints");
            if (spawnRoot == null)
            {
                spawnRoot = new GameObject("SpawnPoints").transform;
                spawnRoot.SetParent(enemiesRoot);
            }
            ClearChildren(spawnRoot);

            Material pathMaterial = GetOrCreateMaterial(
                "Path_Greybox",
                new Color(0.66f, 0.50f, 0.33f)
            );
            Material borderMaterial = GetOrCreateMaterial(
                "Path_Border",
                new Color(0.27f, 0.20f, 0.13f)
            );
            Material buildMaterial = GetOrCreateMaterial(
                "BuildSpot_Greybox",
                new Color(0.92f, 0.67f, 0.18f)
            );

            EnsurePathMeshFolder();

            Vector3[] leftPath =
            {
                P(-20f, 8f), P(-15f, 8f), P(-12f, 12f), P(-6f, 12f),
                P(-4f, 8f), P(-10f, 5f), P(-14f, 1f), P(-12f, -4f),
                P(-7f, -5f), P(-4f, -1f), P(-6f, 3f), P(-2.5f, 2.5f)
            };

            Vector3[] rightPath =
            {
                P(20f, 7f), P(15f, 7f), P(12f, 11f), P(7f, 11f),
                P(4f, 7f), P(10f, 4f), P(14f, 0f), P(12f, -4f),
                P(8f, -5f), P(4f, -1f), P(6f, 3f), P(2.5f, 2.5f)
            };

            Vector3[] bottomPath =
            {
                P(0f, -15f), P(-8f, -14f), P(-15f, -11f), P(-16f, -7f),
                P(-11f, -6f), P(-6f, -9f), P(0f, -11f), P(7f, -11f),
                P(15f, -8f), P(16f, -4f), P(11f, -2f), P(6f, -4f),
                P(2f, -6f), P(0f, -2.5f)
            };

            CreatePath("Path_Left", leftPath, pathsRoot, pathMaterial, borderMaterial);
            CreatePath("Path_Right", rightPath, pathsRoot, pathMaterial, borderMaterial);
            CreatePath("Path_Bottom", bottomPath, pathsRoot, pathMaterial, borderMaterial);

            CreateSpawnPoint("Spawn_Left", leftPath[0], spawnRoot);
            CreateSpawnPoint("Spawn_Right", rightPath[0], spawnRoot);
            CreateSpawnPoint("Spawn_Bottom", bottomPath[0], spawnRoot);

            Vector3[] spots =
            {
                P(-16f, 11.5f), P(-8.5f, 9f), P(-16f, -2f), P(-8f, -8f),
                P(16f, 10.5f), P(8.5f, 8f), P(16f, -1f), P(9f, -8f),
                P(-13f, -13.5f), P(-3f, -13.5f), P(13f, -11.5f), P(5f, -6.5f)
            };

            for (int i = 0; i < spots.Length; i++)
            {
                CreateBuildSpot(i + 1, spots[i], buildSpotsRoot, buildMaterial);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeTransform = pathsRoot;
            Debug.Log("Cats vs Demons smooth garden paths and build spots created.");
        }

        private static void OpenGameSceneIfNeeded()
        {
            if (EditorSceneManager.GetActiveScene().path == ScenePath)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath);
        }

        private static Transform FindRequired(string path)
        {
            GameObject found = GameObject.Find(path);
            return found != null ? found.transform : null;
        }

        private static Vector3 P(float x, float z)
        {
            return new Vector3(x, 0f, z);
        }

        private static void CreatePath(
            string name,
            Vector3[] controlPoints,
            Transform parent,
            Material surfaceMaterial,
            Material borderMaterial)
        {
            Transform pathRoot = new GameObject(name).transform;
            pathRoot.SetParent(parent);

            Vector3[] points = CreateSmoothPoints(controlPoints, CurveSubdivisions);

            CreatePathRibbon(
                name + "_Border",
                points,
                BorderWidth,
                0.025f,
                pathRoot,
                borderMaterial,
                false
            );
            CreatePathRibbon(
                name + "_Surface",
                points,
                PathWidth,
                PathHeight,
                pathRoot,
                surfaceMaterial,
                true
            );

            for (int i = 0; i < points.Length; i++)
            {
                GameObject waypoint = new GameObject($"Joint_{i + 1:000}");
                waypoint.transform.SetParent(pathRoot);
                waypoint.transform.position =
                    points[i] + Vector3.up * PathHeight;
            }
        }

        private static void CreatePathRibbon(
            string name,
            Vector3[] points,
            float width,
            float height,
            Transform parent,
            Material material,
            bool addCollider)
        {
            GameObject ribbon = new GameObject(name);
            ribbon.transform.SetParent(parent);

            Vector3[] vertices = new Vector3[points.Length * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(points.Length - 1) * 6];
            float travelled = 0f;

            for (int index = 0; index < points.Length; index++)
            {
                Vector3 previous = points[Mathf.Max(index - 1, 0)];
                Vector3 next = points[Mathf.Min(index + 1, points.Length - 1)];
                Vector3 tangent = next - previous;
                tangent.y = 0f;
                tangent.Normalize();

                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                Vector3 center = points[index] + Vector3.up * height;
                vertices[index * 2] = center - side * (width * 0.5f);
                vertices[index * 2 + 1] = center + side * (width * 0.5f);

                if (index > 0)
                {
                    travelled += Vector3.Distance(points[index - 1], points[index]);
                }
                uv[index * 2] = new Vector2(0f, travelled * 0.32f);
                uv[index * 2 + 1] = new Vector2(1f, travelled * 0.32f);

                if (index >= points.Length - 1)
                {
                    continue;
                }

                int triangle = index * 6;
                int vertex = index * 2;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            Mesh mesh = new Mesh
            {
                name = name + "_Mesh",
                vertices = vertices,
                uv = uv,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            string meshPath = $"{PathMeshFolder}/{name}.asset";
            Mesh oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (oldMesh != null)
            {
                EditorUtility.CopySerialized(mesh, oldMesh);
                Object.DestroyImmediate(mesh);
                mesh = oldMesh;
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            MeshFilter filter = ribbon.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = ribbon.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            if (addCollider)
            {
                MeshCollider meshCollider = ribbon.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
        }

        private static Vector3[] CreateSmoothPoints(
            Vector3[] controlPoints,
            int subdivisions)
        {
            if (controlPoints == null || controlPoints.Length < 2)
            {
                return controlPoints;
            }

            subdivisions = Mathf.Max(1, subdivisions);
            Vector3[] result = new Vector3[
                (controlPoints.Length - 1) * subdivisions + 1
            ];
            int output = 0;

            for (int segment = 0; segment < controlPoints.Length - 1; segment++)
            {
                Vector3 p0 = controlPoints[Mathf.Max(segment - 1, 0)];
                Vector3 p1 = controlPoints[segment];
                Vector3 p2 = controlPoints[segment + 1];
                Vector3 p3 = controlPoints[
                    Mathf.Min(segment + 2, controlPoints.Length - 1)
                ];

                for (int step = 0; step < subdivisions; step++)
                {
                    float t = step / (float)subdivisions;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    result[output++] = 0.5f * (
                        2f * p1 +
                        (-p0 + p2) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3
                    );
                }
            }

            result[output] = controlPoints[controlPoints.Length - 1];
            return result;
        }

        private static void EnsurePathMeshFolder()
        {
            EnsureFolder(ProjectRoot, "Art");
            EnsureFolder(ProjectRoot + "/Art", "Meshes");
            EnsureFolder(ProjectRoot + "/Art/Meshes", "Paths");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void CreateBuildSpot(
            int index,
            Vector3 position,
            Transform parent,
            Material material)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = $"BuildSpot_{index:00}";
            spot.transform.SetParent(parent);
            spot.transform.position = position + Vector3.up * 0.12f;
            spot.transform.localScale = new Vector3(1.2f, 0.12f, 1.2f);
            spot.GetComponent<Renderer>().sharedMaterial = material;

            if (spot.GetComponent<BuildSpot>() == null)
            {
                spot.AddComponent<BuildSpot>();
            }
        }

        private static void CreateSpawnPoint(
            string name,
            Vector3 position,
            Transform parent)
        {
            GameObject spawn = new GameObject(name);
            spawn.transform.SetParent(parent);
            spawn.transform.position = position + Vector3.up * 0.25f;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{ProjectRoot}/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = name,
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
