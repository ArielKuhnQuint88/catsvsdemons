using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateHouseDefensePrototype
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add House Health and Damage")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            GameObject house = GameObject.Find("Game/Environment/House");
            GameObject systems = GameObject.Find("Game/Systems");

            if (house == null || systems == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "A casa ou o grupo Systems não foi encontrado.",
                    "OK"
                );
                return;
            }

            HouseHealth health = house.GetComponent<HouseHealth>();
            if (health == null)
            {
                health = house.AddComponent<HouseHealth>();
            }

            Transform existingDisplay =
                systems.transform.Find("HouseHealthDisplay");

            if (existingDisplay != null)
            {
                Object.DestroyImmediate(existingDisplay.gameObject);
            }

            CreateHealthDisplay(systems.transform, health);

            EnemyPathFollower demon =
                Object.FindFirstObjectByType<EnemyPathFollower>();

            if (demon != null)
            {
                demon.SetHouseDamage(10);
            }

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = house;
            Debug.Log(
                "House health added. The demon now deals damage at the end of its path."
            );
        }

        private static void CreateHealthDisplay(
            Transform parent,
            HouseHealth health)
        {
            GameObject display = new GameObject("HouseHealthDisplay");
            display.transform.SetParent(parent);
            display.transform.position = new Vector3(0f, 4.2f, 0f);

            GameObject background =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Background";
            background.transform.SetParent(display.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale =
                new Vector3(4.4f, 0.38f, 0.3f);
            RemoveCollider(background);
            background.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial(
                    "HealthBar_Background",
                    new Color(0.08f, 0.08f, 0.08f)
                );

            GameObject fill =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(display.transform);
            fill.transform.localPosition =
                new Vector3(0f, 0f, -0.18f);
            fill.transform.localScale =
                new Vector3(4f, 0.25f, 0.18f);
            RemoveCollider(fill);
            fill.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial(
                    "HealthBar_Green",
                    new Color(0.18f, 0.82f, 0.28f)
                );

            HouseHealthBar bar =
                display.AddComponent<HouseHealthBar>();
            bar.Initialize(health, fill.transform, 4f);
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
