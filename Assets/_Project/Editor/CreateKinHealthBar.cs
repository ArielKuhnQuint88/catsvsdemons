using CatsVsDemons.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateKinHealthBar
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath =
            ProjectRoot + "/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Kin Health Bar")]
        public static void Create()
        {
            if (!OpenGameScene())
            {
                return;
            }

            KinHealth health =
                Object.FindFirstObjectByType<KinHealth>();

            if (health == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "KinHealth não foi encontrado. Execute Add Kin Health primeiro.",
                    "OK"
                );
                return;
            }

            Transform existing =
                health.transform.Find("KinHealthDisplay");

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            CreateDisplay(health);

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = health.gameObject;
            Debug.Log("Kin health bar configured.");
        }

        private static void CreateDisplay(KinHealth health)
        {
            GameObject display = new GameObject("KinHealthDisplay");
            display.transform.SetParent(health.transform);
            display.transform.localPosition =
                new Vector3(0f, 2.3f, 0f);
            display.transform.localScale = Vector3.one;

            GameObject background =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Background";
            background.transform.SetParent(display.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale =
                new Vector3(2f, 0.2f, 0.1f);
            RemoveCollider(background);
            background.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial(
                    "KinHealth_Background",
                    new Color(0.04f, 0.04f, 0.04f)
                );

            GameObject fill =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(display.transform);
            fill.transform.localPosition =
                new Vector3(0f, 0f, -0.08f);
            fill.transform.localScale =
                new Vector3(1.8f, 0.13f, 0.08f);
            RemoveCollider(fill);

            Renderer fillRenderer = fill.GetComponent<Renderer>();
            fillRenderer.sharedMaterial =
                GetOrCreateMaterial(
                    "KinHealth_Green",
                    new Color(0.1f, 0.9f, 0.18f)
                );

            KinHealthBar bar = display.AddComponent<KinHealthBar>();
            bar.Initialize(
                health,
                fill.transform,
                fillRenderer,
                1.8f
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

        private static bool OpenGameScene()
        {
            if (EditorSceneManager.GetActiveScene().path == ScenePath)
            {
                return true;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath);
            return true;
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
