using UnityEngine;

namespace CatsVsDemons.Visuals
{
    /// <summary>The four authored OBJ surfaces keep their identity across import and equipment.</summary>
    public static class KinSurfacePalette
    {
        private static readonly string[] Parts =
        {
            "Kin_ded1cf", "Kin_cb2008", "Kin_3c5861", "Kin_263a46"
        };

        public static bool IsKnownPart(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (string part in Parts)
                if (name.IndexOf(part.Substring(4), System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static string ResolvePart(string name, int index) => IsKnownPart(name)
            ? name : Parts[Mathf.Clamp(index, 0, Parts.Length - 1)];

        public static Color GetColor(string name)
        {
            name = name?.ToLowerInvariant() ?? "";
            if (name.Contains("ded1cf")) return new Color(0.98f, 0.94f, 0.86f);
            if (name.Contains("cb2008")) return new Color(0.80f, 0.08f, 0.04f);
            if (name.Contains("3c5861")) return new Color(0.10f, 0.28f, 0.29f);
            return new Color(0.05f, 0.17f, 0.20f);
        }

        public static void ApplySurfaceDetail(Material material, string partName)
        {
            Texture2D detail = Resources.Load<Texture2D>("Textures/Characters/Kin_SurfaceDetail");
            if (detail != null && material.HasProperty("_DetailMap"))
            {
                material.SetTexture("_DetailMap", detail);
                material.SetFloat("_DetailStrength", partName.Contains("ded1cf") ? 0.8f : 0.55f);
                material.SetFloat("_DetailTiling", 4f);
            }
            if (material.HasProperty("_AmbientFloor")) material.SetFloat("_AmbientFloor", 0.22f);
            if (material.HasProperty("_SpecularStrength")) material.SetFloat("_SpecularStrength", 0.07f);
        }
    }
}
