using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class SetIsometricCamera
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Set Isometric Camera")]
        public static void Apply()
        {
            if (!OpenGameScene())
            {
                return;
            }

            Camera gameCamera = Camera.main;

            if (gameCamera == null)
            {
                GameObject cameraObject =
                    GameObject.Find("Game/Main Camera");

                if (cameraObject != null)
                {
                    gameCamera = cameraObject.GetComponent<Camera>();
                }
            }

            if (gameCamera == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Main Camera não foi encontrada.",
                    "OK"
                );
                return;
            }

            Transform cameraTransform = gameCamera.transform;
            Vector3 target = new Vector3(0f, 0.5f, 1f);

            cameraTransform.position =
                new Vector3(0f, 30f, -34f);
            cameraTransform.rotation =
                Quaternion.LookRotation(
                    target - cameraTransform.position,
                    Vector3.up
                );

            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 18f;
            gameCamera.nearClipPlane = 0.3f;
            gameCamera.farClipPlane = 150f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor =
                new Color(0.16f, 0.17f, 0.19f);

            EditorUtility.SetDirty(gameCamera);
            EditorUtility.SetDirty(cameraTransform);
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = gameCamera.gameObject;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView sceneView = SceneView.lastActiveSceneView;
                sceneView.pivot = target;
                sceneView.rotation = cameraTransform.rotation;
                sceneView.size = 22f;
                sceneView.orthographic = false;
                sceneView.Repaint();
            }

            Debug.Log(
                "Static isometric camera applied and centered on the board."
            );
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
    }
}
