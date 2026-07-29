using CatsVsDemons.Economy;
using CatsVsDemons.Enemies;
using CatsVsDemons.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateCombatPrototype
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Combat and Coins")]
        public static void Create()
        {
            OpenGameSceneIfNeeded();

            GameObject kin =
                GameObject.Find("Game/Player/Kin_Prototype");
            GameObject enemies =
                GameObject.Find("Game/Enemies");
            GameObject systems =
                GameObject.Find("Game/Systems");

            if (kin == null || enemies == null || systems == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Kin, Enemies ou Systems não foi encontrado.",
                    "OK"
                );
                return;
            }

            Transform templateTransform =
                enemies.transform.Find("Demon_Prototype");

            if (templateTransform == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Demon_Prototype não foi encontrado.",
                    "OK"
                );
                return;
            }

            if (kin.GetComponent<KinPrototypeAttack>() == null)
            {
                kin.AddComponent<KinPrototypeAttack>();
            }

            GameObject template = templateTransform.gameObject;
            if (template.GetComponent<EnemyHealth>() == null)
            {
                template.AddComponent<EnemyHealth>();
            }

            if (systems.GetComponent<Wallet>() == null)
            {
                systems.AddComponent<Wallet>();
            }

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = kin;
            Debug.Log(
                "Combat ready. Use Space or left mouse button near a demon."
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
