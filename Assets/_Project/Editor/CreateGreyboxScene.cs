using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsVsDemons.Editor
{
    public static class CreateGreyboxScene
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Create Greybox Scene")]
        public static void Create()
        {
            EnsureFolders();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

            GameObject game = CreateGroup("Game");
            GameObject environment = CreateGroup("Environment", game.transform);
            CreateGroup("Paths", game.transform);
            CreateGroup("BuildSpots", game.transform);
            CreateGroup("Enemies", game.transform);
            CreateGroup("Player", game.transform);
            CreateGroup("Systems", game.transform);

            CreateBoard(environment.transform);
            CreateHouse(environment.transform);
            CreateLighting(environment.transform);
            CreateCamera(game.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = GameObject.Find("House");
            Debug.Log($"Cats vs Demons greybox created at {ScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(ProjectRoot, "Art");
            EnsureFolder(ProjectRoot, "Materials");
            EnsureFolder(ProjectRoot, "Prefabs");
            EnsureFolder(ProjectRoot, "Scenes");
            EnsureFolder(ProjectRoot, "Scripts");
            EnsureFolder(ProjectRoot, "Editor");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static GameObject CreateGroup(string name, Transform parent = null)
        {
            GameObject group = new GameObject(name);
            if (parent != null)
            {
                group.transform.SetParent(parent);
            }

            return group;
        }

        private static void CreateBoard(Transform parent)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Plane);
            board.name = "Board";
            board.transform.SetParent(parent);
            board.transform.position = Vector3.zero;
            board.transform.localScale = new Vector3(4f, 1f, 3f);

            Renderer renderer = board.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreateMaterial(
                "Board_Greybox",
                new Color(0.25f, 0.48f, 0.28f)
            );
        }

        private static void CreateHouse(Transform parent)
        {
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "House";
            house.transform.SetParent(parent);
            house.transform.position = new Vector3(0f, 1.5f, 0f);
            house.transform.localScale = new Vector3(4f, 3f, 4f);

            Renderer renderer = house.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreateMaterial(
                "House_Greybox",
                new Color(0.62f, 0.16f, 0.12f)
            );
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.91f, 0.78f);
            light.shadows = LightShadows.Soft;
        }

        private static void CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(20f, 26f, -20f);
            cameraObject.transform.rotation = Quaternion.Euler(50f, -45f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraObject.AddComponent<AudioListener>();
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
