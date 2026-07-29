using CatsVsDemons.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateEnemyPrototype
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Moving Demon Prototype")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            GameObject enemiesRoot = GameObject.Find("Game/Enemies");
            GameObject firstWaypoint = GameObject.Find(
                "Game/Paths/Path_Left/Joint_01"
            );

            if (enemiesRoot == null || firstWaypoint == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Os caminhos não foram encontrados. Execute primeiro Add Serpentine Paths.",
                    "OK"
                );
                return;
            }

            Transform existing = enemiesRoot.transform.Find("Demon_Prototype");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Recriar demônio?",
                    "O demônio provisório atual será substituído.",
                    "Recriar",
                    "Cancelar"))
                {
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject demon = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            demon.name = "Demon_Prototype";
            demon.transform.SetParent(enemiesRoot.transform);
            demon.transform.position = new Vector3(
                firstWaypoint.transform.position.x,
                1f,
                firstWaypoint.transform.position.z
            );
            demon.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

            demon.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "Demon_Prototype_Purple",
                new Color(0.42f, 0.12f, 0.68f)
            );

            CreateHorn(demon.transform, "Horn_Left", -0.28f);
            CreateHorn(demon.transform, "Horn_Right", 0.28f);

            EnemyPathFollower follower =
                demon.AddComponent<EnemyPathFollower>();
            follower.Configure("Path_Left");

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = demon;
            Debug.Log(
                "Moving demon created. Press Play to watch it follow Path_Left."
            );
        }

        private static void CreateHorn(
            Transform parent,
            string name,
            float x)
        {
            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = name;
            horn.transform.SetParent(parent);
            horn.transform.localPosition = new Vector3(x, 1.05f, 0f);
            horn.transform.localScale = new Vector3(0.22f, 0.42f, 0.22f);

            Collider collider = horn.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            horn.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial(
                    "Demon_Prototype_Horn",
                    new Color(0.78f, 0.2f, 0.86f)
                );
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

        private static Material GetOrCreateMaterial(
            string name,
            Color color)
        {
            string path = $"{ProjectRoot}/Materials/{name}.mat";
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit");

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
