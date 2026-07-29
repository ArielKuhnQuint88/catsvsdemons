using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateGreyboxPaths
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const float PathWidth = 2.2f;
        private const float PathHeight = 0.16f;

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
                new Color(0.58f, 0.47f, 0.34f)
            );
            Material buildMaterial = GetOrCreateMaterial(
                "BuildSpot_Greybox",
                new Color(0.92f, 0.67f, 0.18f)
            );

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

            CreatePath("Path_Left", leftPath, pathsRoot, pathMaterial);
            CreatePath("Path_Right", rightPath, pathsRoot, pathMaterial);
            CreatePath("Path_Bottom", bottomPath, pathsRoot, pathMaterial);

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
            Debug.Log("Cats vs Demons serpentine paths and build spots created.");
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
            Vector3[] points,
            Transform parent,
            Material material)
        {
            Transform pathRoot = new GameObject(name).transform;
            pathRoot.SetParent(parent);

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                Vector3 direction = end - start;
                float length = direction.magnitude;

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"Segment_{i + 1:00}";
                segment.transform.SetParent(pathRoot);
                segment.transform.position = (start + end) * 0.5f + Vector3.up * (PathHeight * 0.5f);
                segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                segment.transform.localScale = new Vector3(PathWidth, PathHeight, length);
                segment.GetComponent<Renderer>().sharedMaterial = material;
            }

            for (int i = 0; i < points.Length; i++)
            {
                GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                joint.name = $"Joint_{i + 1:00}";
                joint.transform.SetParent(pathRoot);
                joint.transform.position = points[i] + Vector3.up * (PathHeight * 0.5f);
                joint.transform.localScale = new Vector3(
                    PathWidth * 0.5f,
                    PathHeight * 0.5f,
                    PathWidth * 0.5f
                );
                joint.GetComponent<Renderer>().sharedMaterial = material;
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
