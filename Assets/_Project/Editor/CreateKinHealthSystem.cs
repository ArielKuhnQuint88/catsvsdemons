using CatsVsDemons.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateKinHealthSystem
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Kin Health")]
        public static void Create()
        {
            if (!OpenGameScene())
            {
                return;
            }

            GameObject playerGroup = GameObject.Find("Game/Player");
            GameObject kin = null;

            if (playerGroup != null && playerGroup.transform.childCount > 0)
            {
                kin = playerGroup.transform.GetChild(0).gameObject;
            }

            if (kin == null)
            {
                kin = GameObject.Find("Kin_Prototype");
            }

            if (kin == null)
            {
                kin = GameObject.Find("Kin");
            }

            if (kin == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Kin não foi encontrado na cena.",
                    "OK"
                );
                return;
            }

            if (kin.GetComponent<KinHealth>() == null)
            {
                kin.AddComponent<KinHealth>();
            }

            kin.tag = "Player";

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = kin;
            Debug.Log("Kin health configured.");
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
