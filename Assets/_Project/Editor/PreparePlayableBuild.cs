using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

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
        private const string AndroidBuildFolder =
            "Builds/CatsVsDemons-Android";
        private const string ApkPath =
            AndroidBuildFolder + "/CatsVsDemons.apk";
        private const string PlayStoreBuildFolder =
            "Builds/CatsVsDemons-PlayStore";

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

        [MenuItem("Tools/Cats vs Demons/Exportar Jogo Leve (Android APK)")]
        public static void ExportLightweightAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(
                BuildTargetGroup.Android,
                BuildTarget.Android))
            {
                EditorUtility.DisplayDialog(
                    "Android não instalado",
                    "No Unity Hub, adicione Android Build Support, " +
                    "Android SDK & NDK Tools e OpenJDK.",
                    "OK"
                );
                return;
            }

            ConfigureScenes();
            ConfigureLightweightAndroidPlayer();
            OptimizeImportedAssets();
            OptimizeAndroidTextures();

            string absoluteFolder = Path.GetFullPath(AndroidBuildFolder);
            Directory.CreateDirectory(absoluteFolder);
            string absoluteApk = Path.GetFullPath(ApkPath);
            if (File.Exists(absoluteApk))
            {
                File.Delete(absoluteApk);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScenePath, ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Exportação Android falhou",
                    "Veja os erros no Console da Unity.",
                    "OK"
                );
                return;
            }

            EditorUtility.RevealInFinder(absoluteApk);
            EditorUtility.DisplayDialog(
                "APK exportado!",
                $"Arquivo pronto para instalar:\n{absoluteApk}\n\n" +
                $"Tamanho: {report.summary.totalSize / 1048576f:0.0} MB",
                "OK"
            );
        }

        [MenuItem("Tools/Cats vs Demons/Exportar para Play Store (AAB)")]
        public static void ExportPlayStoreBundle()
        {
            if (!BuildPipeline.IsBuildTargetSupported(
                BuildTargetGroup.Android,
                BuildTarget.Android))
            {
                EditorUtility.DisplayDialog(
                    "Android não instalado",
                    "Adicione Android Build Support, SDK & NDK Tools e " +
                    "OpenJDK pelo Unity Hub.",
                    "OK"
                );
                return;
            }

            if (!HasReleaseSigningKey())
            {
                bool openSettings = EditorUtility.DisplayDialog(
                    "Assinatura obrigatória",
                    "A Play Store não aceita a chave de teste. Configure " +
                    "um Custom Keystore e informe as senhas em Player > " +
                    "Android > Publishing Settings. Guarde o arquivo e " +
                    "as senhas fora do GitHub.",
                    "Abrir Player Settings",
                    "Cancelar"
                );
                if (openSettings)
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                }
                return;
            }

            ConfigureScenes();
            ConfigurePlayStorePlayer();
            OptimizeImportedAssets();
            OptimizeAndroidTextures();

            string absoluteFolder = Path.GetFullPath(PlayStoreBuildFolder);
            Directory.CreateDirectory(absoluteFolder);
            string safeVersion = PlayerSettings.bundleVersion.Replace('.', '-');
            string relativeBundlePath =
                $"{PlayStoreBuildFolder}/CatsVsDemons-v{safeVersion}-" +
                $"{PlayerSettings.Android.bundleVersionCode}.aab";
            string absoluteBundlePath = Path.GetFullPath(relativeBundlePath);
            if (File.Exists(absoluteBundlePath))
            {
                File.Delete(absoluteBundlePath);
            }

            EditorUserBuildSettings.buildAppBundle = true;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScenePath, ScenePath },
                locationPathName = relativeBundlePath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Exportação para Play Store falhou",
                    "Veja o primeiro erro vermelho no Console da Unity.",
                    "OK"
                );
                return;
            }

            EditorUtility.RevealInFinder(absoluteBundlePath);
            EditorUtility.DisplayDialog(
                "App Bundle pronto!",
                $"Envie este arquivo ao Play Console:\n" +
                $"{absoluteBundlePath}\n\n" +
                $"Versão: {PlayerSettings.bundleVersion}  |  Código: " +
                $"{PlayerSettings.Android.bundleVersionCode}\n" +
                $"Tamanho: {report.summary.totalSize / 1048576f:0.0} MB",
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

        private static void ConfigureLightweightAndroidPlayer()
        {
            PlayerSettings.productName = "Cats vs Demons";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                "com.qipgames.catsvsdemons"
            );
            PlayerSettings.defaultInterfaceOrientation =
                UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground = false;
            PlayerSettings.usePlayerLog = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android,
                ManagedStrippingLevel.Medium
            );
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP
            );
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion =
                AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion =
                AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.SetUseDefaultGraphicsAPIs(
                BuildTarget.Android,
                false
            );
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.Android,
                new[] { GraphicsDeviceType.OpenGLES3 }
            );
            EditorUserBuildSettings.buildAppBundle = false;
        }

        private static void ConfigurePlayStorePlayer()
        {
            ConfigureLightweightAndroidPlayer();
            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ||
                PlayerSettings.bundleVersion == "0.1.0")
            {
                PlayerSettings.bundleVersion = "1.0.0";
            }
            if (PlayerSettings.Android.bundleVersionCode < 1)
            {
                PlayerSettings.Android.bundleVersionCode = 1;
            }

            PlayerSettings.Android.targetSdkVersion =
                AndroidSdkVersions.AndroidApiLevel36;
            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();
        }

        private static bool HasReleaseSigningKey()
        {
            return PlayerSettings.Android.useCustomKeystore &&
                !string.IsNullOrWhiteSpace(
                    PlayerSettings.Android.keystoreName
                ) &&
                !string.IsNullOrWhiteSpace(
                    PlayerSettings.Android.keyaliasName
                ) &&
                !string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) &&
                !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass);
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

        private static void OptimizeAndroidTextures()
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

                TextureImporterPlatformSettings android =
                    importer.GetPlatformTextureSettings("Android");
                android.name = "Android";
                android.overridden = true;
                android.maxTextureSize = path.Contains("/UI/") ? 2048 : 1024;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 60;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
        }
    }
}
