using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Linq;

namespace CatsVsDemons.Editor
{
    public static class PreparePlayableBuild
    {
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

            Debug.Log("Menu e todas as cenas da campanha adicionados ao build.");
            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Menu e campanha de 12 fases preparados para exportação.",
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
                scenes = PlayStoreBuildValidator.ScenePaths,
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
                scenes = PlayStoreBuildValidator.ScenePaths,
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
            ConfigureScenes();
            ConfigurePlayStorePlayer();
            ApplySigningFromEnvironment();
            System.Collections.Generic.List<string> errors =
                PlayStoreBuildValidator.CollectErrors(true);
            if (errors.Count > 0)
            {
                string message = string.Join("\n\n", errors);
                Debug.LogError("Exportação bloqueada:\n" + message);
                if (Application.isBatchMode) throw new BuildFailedException(message);
                EditorUtility.DisplayDialog("Corrigir antes de exportar", message, "OK");
                return;
            }
            BuildPlayStoreBundle();
        }

        private static void BuildPlayStoreBundle()
        {
            OptimizeImportedAssets();
            OptimizeAndroidTextures();

            string absoluteFolder = Path.GetFullPath(PlayStoreBuildFolder);
            Directory.CreateDirectory(absoluteFolder);
            string safeVersion = new string(PlayerSettings.bundleVersion.Select(c =>
                char.IsLetterOrDigit(c) || c == '-' ? c : '-').ToArray());
            string relativeBundlePath =
                $"{PlayStoreBuildFolder}/CatsVsDemons-v{safeVersion}-" +
                $"{PlayerSettings.Android.bundleVersionCode}.aab";
            string absoluteBundlePath = Path.GetFullPath(relativeBundlePath);
            if (File.Exists(absoluteBundlePath))
                throw new BuildFailedException("Já existe um AAB desta versão em " +
                    absoluteBundlePath + ". Aumente Bundle Version Code ou mova o arquivo anterior.");

            EditorUserBuildSettings.buildAppBundle = true;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = PlayStoreBuildValidator.ScenePaths,
                locationPathName = relativeBundlePath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode)
                    throw new BuildFailedException("A Unity não conseguiu gerar o AAB. Consulte o log.");
                EditorUtility.DisplayDialog(
                    "Exportação para Play Store falhou",
                    "Veja o primeiro erro vermelho no Console da Unity.",
                    "OK"
                );
                return;
            }

            PlayStoreBuildValidator.VerifyBundle(absoluteBundlePath);
            string receipt = PlayStoreBuildValidator.WriteBuildReceipt(absoluteBundlePath);
            Debug.Log("AAB gerado e bibliotecas ARM64 verificadas: " + absoluteBundlePath +
                "\nRelatório: " + receipt);
            if (Application.isBatchMode) return;

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
            EditorBuildSettings.scenes = PlayStoreBuildValidator.ScenePaths
                .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            PlayStoreBuildValidator.EnsureRuntimeShaders();
        }

        private static void ConfigureLightweightPlayer()
        {
            PlayerSettings.productName = "Cats vs Demons";
            PlayerSettings.companyName = "Quint Games";
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
            PlayerSettings.companyName = "Quint Games";
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
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
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
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Cats vs Demons/Preparar Android para Play Console")]
        public static void PreparePlayConsole()
        {
            ConfigureScenes();
            ConfigurePlayStorePlayer();
            OptimizeImportedAssets();
            OptimizeAndroidTextures();
            PlayStoreBuildValidator.ValidateFromMenu();
        }

        private static void ApplySigningFromEnvironment()
        {
            string keystore = Environment.GetEnvironmentVariable("CVD_KEYSTORE_PATH");
            if (!string.IsNullOrWhiteSpace(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
            }
            string alias = Environment.GetEnvironmentVariable("CVD_KEY_ALIAS");
            if (!string.IsNullOrWhiteSpace(alias)) PlayerSettings.Android.keyaliasName = alias;
            string storePassword = Environment.GetEnvironmentVariable("CVD_KEYSTORE_PASSWORD");
            if (!string.IsNullOrEmpty(storePassword)) PlayerSettings.Android.keystorePass = storePassword;
            string keyPassword = Environment.GetEnvironmentVariable("CVD_KEY_PASSWORD");
            if (!string.IsNullOrEmpty(keyPassword)) PlayerSettings.Android.keyaliasPass = keyPassword;
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

                bool readable = CharacterRigModelImporter.IsCharacterModel(path);
                bool changed = importer.meshCompression !=
                        ModelImporterMeshCompression.Medium ||
                    !importer.optimizeMeshPolygons ||
                    !importer.optimizeMeshVertices ||
                    importer.isReadable != readable;
                if (changed)
                {
                    importer.meshCompression =
                        ModelImporterMeshCompression.Medium;
                    importer.optimizeMeshPolygons = true;
                    importer.optimizeMeshVertices = true;
                    importer.isReadable = readable;
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
                android.androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit;
                TextureImporterPlatformSettings previous = importer.GetPlatformTextureSettings("Android");
                if (previous.overridden && previous.maxTextureSize == android.maxTextureSize &&
                    previous.format == android.format && previous.compressionQuality == android.compressionQuality &&
                    previous.androidETC2FallbackOverride == android.androidETC2FallbackOverride) continue;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
        }
    }
}
