using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class PreparePlayableBuild
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cats vs Demons/Prepare Playable Build")]
        public static void Prepare()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes
                );

            bool found = false;

            for (int index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path != ScenePath)
                {
                    continue;
                }

                scenes[index] =
                    new EditorBuildSettingsScene(ScenePath, true);
                found = true;
                break;
            }

            if (!found)
            {
                scenes.Insert(
                    0,
                    new EditorBuildSettingsScene(ScenePath, true)
                );
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();

            Debug.Log("Game scene added to Build Settings.");
            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Cena Game preparada para jogar e reiniciar.",
                "OK"
            );
        }
    }
}
