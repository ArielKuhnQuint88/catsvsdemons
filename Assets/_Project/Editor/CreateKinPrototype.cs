using CatsVsDemons.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateKinPrototype
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Kin Prototype")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            GameObject playerRootObject = GameObject.Find("Game/Player");
            if (playerRootObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "A cena Game não possui o grupo Player esperado.",
                    "OK"
                );
                return;
            }

            Transform existing = playerRootObject.transform.Find("Kin_Prototype");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Recriar Kin?",
                    "O protótipo atual de Kin será substituído.",
                    "Recriar",
                    "Cancelar"))
                {
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject kin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kin.name = "Kin_Prototype";
            kin.transform.SetParent(playerRootObject.transform);
            kin.transform.position = new Vector3(0f, 1f, -5f);

            CapsuleCollider capsuleCollider = kin.GetComponent<CapsuleCollider>();
            Object.DestroyImmediate(capsuleCollider);

            CharacterController characterController = kin.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = Vector3.zero;
            characterController.stepOffset = 0.3f;

            kin.AddComponent<KinPrototypeController>();

            Renderer renderer = kin.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreateMaterial(
                "Kin_Prototype_White",
                new Color(0.96f, 0.96f, 0.92f)
            );

            CreateBelt(kin.transform);
            CreateSword(kin.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = kin;
            Debug.Log("Kin prototype created. Press Play and use WASD or arrow keys.");
        }

        private static void CreateBelt(Transform parent)
        {
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Red_Belt";
            belt.transform.SetParent(parent);
            belt.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            belt.transform.localScale = new Vector3(1.05f, 0.22f, 1.05f);

            Object.DestroyImmediate(belt.GetComponent<BoxCollider>());
            belt.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "Kin_Prototype_Red",
                new Color(0.72f, 0.08f, 0.08f)
            );
        }

        private static void CreateSword(Transform parent)
        {
            GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Golden_Sword";
            sword.transform.SetParent(parent);
            sword.transform.localPosition = new Vector3(0.65f, 0f, 0f);
            sword.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            sword.transform.localScale = new Vector3(0.12f, 1.35f, 0.12f);

            Object.DestroyImmediate(sword.GetComponent<BoxCollider>());
            sword.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "Kin_Prototype_Gold",
                new Color(0.95f, 0.62f, 0.08f)
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
