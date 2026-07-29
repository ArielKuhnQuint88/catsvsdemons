using CatsVsDemons.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateEnemyHealthBars
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath =
            ProjectRoot + "/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Enemy Health Bars")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            GameObject enemies =
                GameObject.Find("Game/Enemies");

            if (enemies == null)
            {
                ShowMissingTemplate();
                return;
            }

            Transform templateTransform =
                enemies.transform.Find("Demon_Prototype");

            if (templateTransform == null)
            {
                ShowMissingTemplate();
                return;
            }

            GameObject template = templateTransform.gameObject;
            EnemyHealth health =
                template.GetComponent<EnemyHealth>();

            if (health == null)
            {
                health = template.AddComponent<EnemyHealth>();
            }

            Transform existing =
                template.transform.Find("EnemyHealthDisplay");

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            CreateDisplay(template.transform, health);

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = template;
            Debug.Log(
                "Enemy health bars added to the wave template."
            );
        }

        private static void CreateDisplay(
            Transform parent,
            EnemyHealth health)
        {
            GameObject display =
                new GameObject("EnemyHealthDisplay");

            display.transform.SetParent(parent);
            display.transform.localPosition =
                new Vector3(0f, 1.55f, 0f);
            display.transform.localScale = Vector3.one;

            GameObject background =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            background.name = "Background";
            background.transform.SetParent(display.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale =
                new Vector3(1.6f, 0.16f, 0.1f);
            RemoveCollider(background);
            background.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial(
                    "EnemyHealth_Background",
                    new Color(0.05f, 0.05f, 0.05f)
                );

            GameObject fill =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            fill.name = "Fill";
            fill.transform.SetParent(display.transform);
            fill.transform.localPosition =
                new Vector3(0f, 0f, -0.08f);
            fill.transform.localScale =
                new Vector3(1.4f, 0.1f, 0.08f);
            RemoveCollider(fill);
            fill.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial(
                    "EnemyHealth_Red",
                    new Color(0.9f, 0.08f, 0.08f)
                );

            EnemyHealthBar bar =
                display.AddComponent<EnemyHealthBar>();

            bar.Initialize(health, fill.transform, 1.4f);
        }

        private static void ShowMissingTemplate()
        {
            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Demon_Prototype não foi encontrado.",
                "OK"
            );
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
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
