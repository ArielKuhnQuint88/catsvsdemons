using UnityEditor;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public sealed class CharacterRigModelImporter : AssetPostprocessor
    {
        private static readonly string[] CharacterModels =
        {
            "Assets/_Project/Resources/Models/Kin.obj",
            "Assets/_Project/Resources/Models/DemonPoerix.obj",
            "Assets/_Project/Resources/Models/DemonSono.obj",
            "Assets/_Project/Resources/Models/DemonFlamurk.obj"
        };

        private void OnPreprocessModel()
        {
            if (!IsCharacterModel(assetPath))
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.isReadable = true;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
        }

        [MenuItem("Tools/Cats vs Demons/Prepare Automatic Rigs")]
        public static void Prepare()
        {
            int prepared = 0;

            foreach (string path in CharacterModels)
            {
                ModelImporter importer =
                    AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer == null)
                {
                    Debug.LogWarning($"Character model not found: {path}");
                    continue;
                }

                importer.isReadable = true;
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.importTangents =
                    ModelImporterTangents.CalculateMikk;
                importer.meshCompression =
                    ModelImporterMeshCompression.Medium;
                importer.SaveAndReimport();
                prepared++;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                $"{prepared} personagens preparados para o auto-rig. " +
                "Agora aperte Play.",
                "OK"
            );
        }

        private static bool IsCharacterModel(string path)
        {
            foreach (string model in CharacterModels)
            {
                if (path == model)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
