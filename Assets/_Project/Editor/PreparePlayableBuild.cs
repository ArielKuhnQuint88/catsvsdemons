using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class PreparePlayableBuild
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Game.unity";
        private const string MenuScenePath =
            "Assets/_Project/Scenes/MainMenu.unity";
        private const string BuildFolder = "Builds/CatsVsDemons-Windows";
        private const string ExecutablePath =
            BuildFolder + "/CatsVsDemons.exe";

        [MenuItem("Tools/Cats vs Demons/Prepare Playable Build")]
        public static void Prepare()
        {
            ConfigureScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("Game scene added to Build Settings.");
            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Cena Game preparada para jogar e reiniciar.",
                "OK"
            );
        }

        [MenuItem("Tools/Cats vs Demons/Exportar Jogo Leve (Windows)")]
        public static void ExportLightweightWindows()
        {
            ConfigureScenes();
            ConfigureLightweightPlayer();
            OptimizeImportedAssets();

            string absoluteBuildFolder = Path.GetFullPath(BuildFolder);
            if (Directory.Exists(absoluteBuildFolder))
            {
                Directory.Delete(absoluteBuildFolder, true);
            }
            Directory.CreateDirectory(absoluteBuildFolder);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScenePath, ScenePath },
                locationPathName = ExecutablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Exportação falhou",
                    "Veja os erros no Console da Unity.",
                    "OK"
                );
                return;
            }

            string zipPath = Path.GetFullPath(
                "Builds/CatsVsDemons-Windows.zip"
            );
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(
                absoluteBuildFolder,
                zipPath,
                System.IO.Compression.CompressionLevel.Optimal,
                false
            );

            EditorUtility.RevealInFinder(zipPath);
            EditorUtility.DisplayDialog(
                "Jogo exportado!",
                $"Arquivo pronto para enviar:\n{zipPath}\n\n" +
                $"Tamanho do build: {report.summary.totalSize / 1048576f:0.0} MB",
                "OK"
            );
        }

        private static void ConfigureScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static void ConfigureLightweightPlayer()
        {
            PlayerSettings.productName = "Cats vs Demons";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.usePlayerLog = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Standalone,
                ManagedStrippingLevel.Medium
            );
        }

        private static void OptimizeImportedAssets()
        {
            string[] textureGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/_Project" }
            );
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path)
                    as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                int maximumSize = path.Contains("Textures/Characters")
                    ? 1024
                    : 2048;
                bool changed = importer.maxTextureSize != maximumSize ||
                    importer.textureCompression !=
                        TextureImporterCompression.Compressed ||
                    importer.compressionQuality != 60 ||
                    importer.mipmapEnabled == path.Contains("/UI/");
                importer.maxTextureSize = maximumSize;
                importer.textureCompression =
                    TextureImporterCompression.Compressed;
                importer.compressionQuality = 60;
                importer.mipmapEnabled = !path.Contains("/UI/");
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            string[] modelGuids = AssetDatabase.FindAssets(
                "t:Model",
                new[] { "Assets/_Project" }
            );
            foreach (string guid in modelGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path)
                    as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = importer.meshCompression !=
                        ModelImporterMeshCompression.Medium ||
                    !importer.optimizeMeshPolygons ||
                    !importer.optimizeMeshVertices ||
                    importer.isReadable;
                if (changed)
                {
                    importer.meshCompression =
                        ModelImporterMeshCompression.Medium;
                    importer.optimizeMeshPolygons = true;
                    importer.optimizeMeshVertices = true;
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}
