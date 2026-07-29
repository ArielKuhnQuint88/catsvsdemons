using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
using CatsVsDemons.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class CreateFirstTowerSystem
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Add Coins and First Tower")]
        public static void Create()
        {
            if (!OpenGameScene())
            {
                return;
            }

            GameObject systems = GameObject.Find("Game/Systems");
            GameObject buildSpots = GameObject.Find("Game/BuildSpots");

            if (systems == null || buildSpots == null)
            {
                EditorUtility.DisplayDialog(
                    "Cats vs Demons",
                    "Systems ou BuildSpots não foram encontrados na cena.",
                    "OK"
                );
                return;
            }

            Wallet wallet = Object.FindFirstObjectByType<Wallet>();

            if (wallet == null)
            {
                wallet = systems.AddComponent<Wallet>();
            }

            SerializedObject walletData = new SerializedObject(wallet);
            SerializedProperty startingCoins =
                walletData.FindProperty("startingCoins");

            if (startingCoins != null)
            {
                startingCoins.intValue = 20;
                walletData.ApplyModifiedProperties();
            }

            if (systems.GetComponent<CoinHud>() == null)
            {
                systems.AddComponent<CoinHud>();
            }

            int configuredSpots = 0;

            for (int index = 0; index < buildSpots.transform.childCount; index++)
            {
                Transform spot = buildSpots.transform.GetChild(index);

                if (spot.GetComponent<BuildSpot>() == null)
                {
                    spot.gameObject.AddComponent<BuildSpot>();
                }

                if (spot.GetComponent<Collider>() == null)
                {
                    BoxCollider collider =
                        spot.gameObject.AddComponent<BoxCollider>();
                    collider.size = Vector3.one;
                }

                configuredSpots++;
            }

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = systems;
            Debug.Log(
                $"Moedas e primeira torre configuradas em {configuredSpots} pontos."
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
