using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CatsVsDemons.Editor
{
    public static class PlayStoreBuildValidator
    {
        public const string PackageName = "com.qipgames.catsvsdemons";
        public static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Game.unity",
            "Assets/_Project/Scenes/Game_BambooGrove.unity",
            "Assets/_Project/Scenes/Game_EclipseSanctuary.unity"
        };
        private static readonly string[] RuntimeShaders =
        {
            "CatsVsDemons/StylizedModel", "CatsVsDemons/GardenWater",
            "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit"
        };
        private static readonly string[] RequiredResources =
        {
            "Models/Kin", "Models/DemonSono", "Models/DemonPoerix", "Models/DemonFlamurk",
            "Models/Bonsai", "Models/StoneLantern", "UI/TowerPortalData", "UI/TowerBonsaiData",
            "UI/TowerLanternData", "UI/EndingVictoryData", "UI/EndingDefeatData",
            "UI/HouseInterior", "UI/HouseShopClothes", "UI/HouseShopAccessories"
        };

        [MenuItem("Tools/Cats vs Demons/Verificar prontidao para Play Console")]
        public static void ValidateFromMenu()
        {
            List<string> errors = CollectErrors(false);
            string result = errors.Count == 0
                ? "Configuração e conteúdo verificados. Informe as senhas da chave de upload " +
                    "antes de exportar. O AAB ainda precisa ser gerado e testado em Android."
                : string.Join("\n\n", errors);
            Debug.Log("[Play Console] " + result);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Preparação do Play Console", result, "OK");
        }

        public static List<string> CollectErrors(bool requireSigning)
        {
            List<string> errors = new();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                errors.Add("Instale Android Build Support, SDK & NDK Tools e OpenJDK no Unity Hub.");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != PackageName)
                errors.Add("Mantenha o identificador da aplicação: " + PackageName);
            if ((int)PlayerSettings.Android.targetSdkVersion < 36)
                errors.Add("O Target API Level precisa ser 36 ou superior.");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64 ||
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                errors.Add("Use ARM64 e IL2CPP para este perfil de exportação.");
            if (PlayerSettings.Android.bundleVersionCode < 1 || string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
                errors.Add("Informe versão e Bundle Version Code positivo, superior ao último enviado ao Console.");
            foreach (string path in ScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    errors.Add("Cena da campanha ausente: " + path);
                if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path))
                    errors.Add("Inclua a cena no build: " + path);
            }
            foreach (string resource in RequiredResources)
                if (Resources.Load(resource) == null) errors.Add("Recurso de produção ausente: " + resource);
            SerializedObject graphics = GetGraphicsSettings();
            SerializedProperty included = graphics?.FindProperty("m_AlwaysIncludedShaders");
            foreach (string name in RuntimeShaders)
            {
                Shader shader = Shader.Find(name);
                if (shader == null) { errors.Add("Shader ausente: " + name); continue; }
                bool found = false;
                if (included != null)
                    for (int index = 0; index < included.arraySize; index++)
                        if (included.GetArrayElementAtIndex(index).objectReferenceValue == shader) found = true;
                if (!found) errors.Add("Shader runtime precisa estar em Always Included Shaders: " + name);
            }
            foreach (string name in new[] { "Kin", "DemonSono", "DemonPoerix", "DemonFlamurk" })
            {
                string path = "Assets/_Project/Resources/Models/" + name + ".obj";
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || !importer.isReadable)
                    errors.Add("Habilite Read/Write para as animações de " + name + ".");
            }
            if (requireSigning)
            {
                if (!PlayerSettings.Android.useCustomKeystore ||
                    string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) ||
                    !File.Exists(PlayerSettings.Android.keystoreName))
                    errors.Add("Selecione a chave de upload existente em Player > Android > Publishing Settings.");
                if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName) ||
                    string.Equals(PlayerSettings.Android.keyaliasName, "androiddebugkey", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Selecione o alias da chave de upload; a chave debug não é aceita.");
                if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ||
                    string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                    errors.Add("Informe as senhas do keystore e do alias na Unity (não no repositório).");
            }
            return errors;
        }

        public static void EnsureRuntimeShaders()
        {
            SerializedObject settings = GetGraphicsSettings();
            if (settings == null) throw new BuildFailedException("GraphicsSettings não encontrado.");
            SerializedProperty included = settings.FindProperty("m_AlwaysIncludedShaders");
            if (included == null) throw new BuildFailedException("Lista de shaders de produção ausente.");
            foreach (string name in RuntimeShaders)
            {
                Shader shader = Shader.Find(name);
                if (shader == null) throw new BuildFailedException("Shader obrigatório não encontrado: " + name);
                bool found = false;
                for (int index = 0; index < included.arraySize; index++)
                    if (included.GetArrayElementAtIndex(index).objectReferenceValue == shader) found = true;
                if (found) continue;
                int last = included.arraySize;
                included.InsertArrayElementAtIndex(last);
                included.GetArrayElementAtIndex(last).objectReferenceValue = shader;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static SerializedObject GetGraphicsSettings()
        {
            UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            return asset != null ? new SerializedObject(asset) : null;
        }

        public static void VerifyBundle(string path)
        {
            List<string> errors = AndroidBundleInspector.Inspect(path);
            if (errors.Count > 0)
                throw new BuildFailedException("O AAB foi gerado, mas falhou na validação:\n" + string.Join("\n", errors));
        }

        [Serializable]
        private sealed class BuildReceipt
        {
            public string file;
            public string unity;
            public string package;
            public string version;
            public int versionCode;
            public int targetApi;
            public string architecture;
            public string[] scenes;
            public string sha256;
            public string createdUtc;
            public string nativeValidation;
            public string deviceValidation;
        }

        public static string WriteBuildReceipt(string bundlePath)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(bundlePath);
            string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            BuildReceipt receipt = new()
            {
                file = Path.GetFileName(bundlePath), unity = Application.unityVersion,
                package = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                version = PlayerSettings.bundleVersion, versionCode = PlayerSettings.Android.bundleVersionCode,
                targetApi = (int)PlayerSettings.Android.targetSdkVersion, architecture = "ARM64 / IL2CPP",
                scenes = ScenePaths, sha256 = hash, createdUtc = DateTime.UtcNow.ToString("O"),
                nativeValidation = "ELF64 ARM64: segmentos LOAD com alinhamento de 16 KB; assinatura presente",
                deviceValidation = "Pendente: teste em dispositivo e relatório de pré-lançamento do Play Console"
            };
            string path = Path.ChangeExtension(bundlePath, ".build.json");
            File.WriteAllText(path, JsonUtility.ToJson(receipt, true));
            return path;
        }
    }
}
