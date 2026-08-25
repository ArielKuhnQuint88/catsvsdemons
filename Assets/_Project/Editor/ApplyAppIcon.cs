using UnityEditor;
using UnityEngine;

namespace CatsVsDemons.EditorTools
{
    [InitializeOnLoad]
    public static class ApplyAppIcon
    {
        private const string IconPath = "Assets/_Project/Art/UI/AppIcon_Kin.png";
        private const string SessionKey = "CatsVsDemons.AppIconApplied";

        static ApplyAppIcon()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += Apply;
        }

        [MenuItem("Tools/Cats vs Demons/Aplicar ícone do aplicativo")]
        public static void Apply()
        {
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogError($"Não foi possível carregar o ícone em {IconPath}.");
                return;
            }

            SetApplicationIcons(BuildTargetGroup.Android, icon);
            SetApplicationIcons(BuildTargetGroup.Standalone, icon);

            AssetDatabase.SaveAssets();
            Debug.Log("Ícone do Kin aplicado ao Android e ao desktop.");
        }

        private static void SetApplicationIcons(BuildTargetGroup targetGroup, Texture2D icon)
        {
            var sizes = PlayerSettings.GetIconSizesForTargetGroup(targetGroup, IconKind.Application);
            if (sizes == null || sizes.Length == 0)
                return;

            var icons = new Texture2D[sizes.Length];
            for (var i = 0; i < icons.Length; i++)
                icons[i] = icon;

            PlayerSettings.SetIconsForTargetGroup(targetGroup, icons, IconKind.Application);
        }
    }
}
