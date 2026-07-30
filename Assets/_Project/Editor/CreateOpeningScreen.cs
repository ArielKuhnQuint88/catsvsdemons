using System.Collections.Generic;
using CatsVsDemons.CameraSystem;
using CatsVsDemons.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsVsDemons.Editor
{
    public static class CreateOpeningScreen
    {
        private const string GameScene =
            "Assets/_Project/Scenes/Game.unity";
        private const string MenuScene =
            "Assets/_Project/Scenes/MainMenu.unity";
        private const string BackgroundPath =
            "Assets/_Project/Resources/UI/OpeningBackground.png";

        [MenuItem("Tools/Cats vs Demons/Create Opening Screen")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureGameCamera();
            CreateMenuScene();
            ConfigureBuildSettings();

            EditorSceneManager.OpenScene(MenuScene);

            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Tela de abertura criada. Aperte Play para testar.",
                "OK"
            );
        }

        private static void ConfigureGameCamera()
        {
            Scene game = EditorSceneManager.OpenScene(
                GameScene,
                OpenSceneMode.Single
            );

            Camera camera = Camera.main;

            if (camera == null)
            {
                Debug.LogError("Main Camera não encontrada na cena Game.");
                return;
            }

            if (camera.GetComponent<CameraModeController>() == null)
            {
                camera.gameObject.AddComponent<CameraModeController>();
            }

            EditorSceneManager.MarkSceneDirty(game);
            EditorSceneManager.SaveScene(game, GameScene);
        }

        private static void CreateMenuScene()
        {
            Scene menu = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.04f, 0.12f);
            camera.orthographic = true;

            cameraObject.AddComponent<AudioListener>();

            GameObject menuController =
                new GameObject("Opening Screen");

            MainMenuUI menuUI =
                menuController.AddComponent<MainMenuUI>();

            AssetDatabase.ImportAsset(
                BackgroundPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate
            );

            Texture2D background =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    BackgroundPath
                );

            if (background == null)
            {
                string[] matches =
                    AssetDatabase.FindAssets(
                        "OpeningBackground t:Texture2D"
                    );

                if (matches.Length > 0)
                {
                    string foundPath =
                        AssetDatabase.GUIDToAssetPath(matches[0]);
                    background =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(
                            foundPath
                        );
                }
            }

            if (background == null)
            {
                EditorUtility.DisplayDialog(
                    "Imagem não encontrada",
                    "A Unity não conseguiu importar OpeningBackground.png.",
                    "OK"
                );
                Debug.LogError(
                    $"Opening background not found: {BackgroundPath}"
                );
            }
            else
            {
                menuUI.SetBackground(background);
                EditorUtility.SetDirty(menuUI);
                Debug.Log(
                    $"Opening background applied: " +
                    AssetDatabase.GetAssetPath(background)
                );
            }

            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu, MenuScene);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>
                {
                    new EditorBuildSettingsScene(MenuScene, true),
                    new EditorBuildSettingsScene(GameScene, true)
                };

            foreach (EditorBuildSettingsScene existing in
                EditorBuildSettings.scenes)
            {
                if (existing.path == MenuScene ||
                    existing.path == GameScene)
                {
                    continue;
                }

                scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
