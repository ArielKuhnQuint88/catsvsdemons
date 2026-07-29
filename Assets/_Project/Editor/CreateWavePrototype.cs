using CatsVsDemons.Enemies;
using CatsVsDemons.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateWavePrototype
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Test Waves")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            GameObject systems = GameObject.Find("Game/Systems");
            GameObject enemies = GameObject.Find("Game/Enemies");
            GameObject template = GameObject.Find(
                "Game/Enemies/Demon_Prototype"
            );

            if (systems == null || enemies == null || template == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Systems, Enemies ou Demon_Prototype não foi encontrado.",
                    "OK"
                );
                return;
            }

            Transform existing =
                systems.transform.Find("WaveSpawner");

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            EnemyPathFollower follower =
                template.GetComponent<EnemyPathFollower>();

            if (follower != null)
            {
                follower.SetHouseDamage(10);
            }

            template.SetActive(false);

            GameObject spawnerObject = new GameObject("WaveSpawner");
            spawnerObject.transform.SetParent(systems.transform);

            EnemyWaveSpawner spawner =
                spawnerObject.AddComponent<EnemyWaveSpawner>();

            spawner.Initialize(template, enemies.transform);

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = spawnerObject;
            Debug.Log(
                "Test waves created: 5, 7 and 9 enemies."
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
    }
}
