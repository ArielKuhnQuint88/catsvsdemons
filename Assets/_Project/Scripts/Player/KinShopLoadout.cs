using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Player
{
    public sealed class KinShopLoadout : MonoBehaviour
    {
        private enum PaletteSlot
        {
            Fur,
            Primary,
            Secondary,
            Dark
        }

        private sealed class PaletteEntry
        {
            public Material Material;
            public PaletteSlot Slot;
        }

        private readonly List<PaletteEntry> palette = new();
        private readonly List<Material> ownedMaterials = new();
        private Transform accessoryRoot;
        private string appliedOutfit;
        private string appliedAccessory;

        public void Apply(string outfitId, string accessoryId)
        {
            outfitId = string.IsNullOrEmpty(outfitId)
                ? "samurai_vermelho"
                : outfitId;
            accessoryId ??= string.Empty;

            if (appliedOutfit == outfitId &&
                appliedAccessory == accessoryId &&
                accessoryRoot != null)
            {
                return;
            }

            EnsurePaletteMaterials();
            ApplyOutfitPalette(outfitId);
            RebuildAccessory(accessoryId);
            appliedOutfit = outfitId;
            appliedAccessory = accessoryId;
        }

        private void EnsurePaletteMaterials()
        {
            if (palette.Count > 0)
            {
                return;
            }

            Transform model = transform.Find("GameplayModel");
            if (model == null)
            {
                return;
            }

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer modelRenderer in renderers)
            {
                Material[] source = modelRenderer.sharedMaterials;
                Material[] instances = new Material[source.Length];
                for (int index = 0; index < source.Length; index++)
                {
                    Material original = source[index];
                    if (original == null)
                    {
                        continue;
                    }

                    Material instance = new(original)
                    {
                        name = $"Kin Shop {original.name}",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    instances[index] = instance;
                    ownedMaterials.Add(instance);
                    palette.Add(new PaletteEntry
                    {
                        Material = instance,
                        Slot = DetectSlot(original)
                    });
                }
                modelRenderer.sharedMaterials = instances;
            }
        }

        private static PaletteSlot DetectSlot(Material material)
        {
            string label = material.name.ToLowerInvariant();
            if (label.Contains("ded1cf")) return PaletteSlot.Fur;
            if (label.Contains("cb2008")) return PaletteSlot.Primary;
            if (label.Contains("3c5861")) return PaletteSlot.Secondary;
            if (label.Contains("263a46")) return PaletteSlot.Dark;

            Color color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.HasProperty("_Color")
                    ? material.GetColor("_Color")
                    : Color.gray;
            float brightness = color.r + color.g + color.b;
            if (brightness > 2.15f) return PaletteSlot.Fur;
            if (color.r > color.g * 1.45f) return PaletteSlot.Primary;
            return brightness < 0.45f
                ? PaletteSlot.Dark
                : PaletteSlot.Secondary;
        }

        private void ApplyOutfitPalette(string outfitId)
        {
            GetPalette(outfitId, out Color primary, out Color secondary,
                out Color dark, out Color accent);
            Color fur = new(0.98f, 0.94f, 0.86f);

            foreach (PaletteEntry entry in palette)
            {
                Color color = entry.Slot switch
                {
                    PaletteSlot.Fur => fur,
                    PaletteSlot.Primary => primary,
                    PaletteSlot.Secondary => secondary,
                    _ => dark
                };
                SetMaterialColors(entry.Material, color, accent);
            }
        }

        private static void GetPalette(
            string outfitId,
            out Color primary,
            out Color secondary,
            out Color dark,
            out Color accent)
        {
            primary = new Color(0.72f, 0.055f, 0.04f);
            secondary = new Color(0.06f, 0.23f, 0.24f);
            dark = new Color(0.035f, 0.10f, 0.12f);
            accent = new Color(1f, 0.63f, 0.12f);

            switch (outfitId)
            {
                case "ninja_meia_noite":
                    primary = new Color(0.18f, 0.08f, 0.42f);
                    secondary = new Color(0.035f, 0.09f, 0.24f);
                    dark = new Color(0.018f, 0.018f, 0.055f);
                    accent = new Color(0.22f, 0.48f, 1f);
                    break;
                case "guardiao_bonsai":
                    primary = new Color(0.12f, 0.38f, 0.14f);
                    secondary = new Color(0.78f, 0.72f, 0.48f);
                    dark = new Color(0.035f, 0.14f, 0.065f);
                    accent = new Color(0.72f, 0.88f, 0.24f);
                    break;
                case "mestre_lanternas":
                    primary = new Color(0.88f, 0.18f, 0.025f);
                    secondary = new Color(0.18f, 0.055f, 0.025f);
                    dark = new Color(0.055f, 0.025f, 0.02f);
                    accent = new Color(1f, 0.55f, 0.05f);
                    break;
                case "gato_domestico":
                    primary = new Color(0.86f, 0.76f, 0.58f);
                    secondary = new Color(0.10f, 0.34f, 0.58f);
                    dark = new Color(0.06f, 0.16f, 0.28f);
                    accent = new Color(0.88f, 0.10f, 0.08f);
                    break;
                case "ronin_espiritual":
                    primary = new Color(0.025f, 0.11f, 0.28f);
                    secondary = new Color(0.05f, 0.32f, 0.68f);
                    dark = new Color(0.01f, 0.025f, 0.08f);
                    accent = new Color(0.22f, 0.72f, 1f);
                    break;
            }
        }

        private static void SetMaterialColors(
            Material material,
            Color baseColor,
            Color accent)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);
            if (material.HasProperty("_SecondaryColor"))
                material.SetColor("_SecondaryColor",
                    Color.Lerp(baseColor, Color.white, 0.22f));
            if (material.HasProperty("_AccentColor"))
                material.SetColor("_AccentColor", accent);
            if (material.HasProperty("_RimColor"))
                material.SetColor("_RimColor",
                    Color.Lerp(accent, Color.white, 0.35f));
        }

        private void RebuildAccessory(string accessoryId)
        {
            if (accessoryRoot != null)
            {
                Destroy(accessoryRoot.gameObject);
            }

            GameObject root = new("Equipped Shop Accessory");
            root.transform.SetParent(transform, false);
            accessoryRoot = root.transform;

            switch (accessoryId)
            {
                case "faixa_samurai":
                    BuildHeadband();
                    break;
                case "oculos":
                    BuildGlasses();
                    break;
                case "coleira_sino":
                    BuildCollar();
                    break;
                case "mochila_peixe":
                    BuildFishBackpack();
                    break;
                case "amuleto_protecao":
                    BuildAmulet();
                    break;
                case "asas_espirituais":
                    BuildWings();
                    break;
                case "passos_magicos":
                    BuildMagicSteps();
                    break;
            }
        }

        private void BuildHeadband()
        {
            Color red = new(0.72f, 0.045f, 0.035f);
            Part("Samurai Headband", PrimitiveType.Cube,
                new Vector3(0f, 0.66f, 0.02f),
                new Vector3(0.78f, 0.10f, 0.62f), red);
            Part("Headband Knot", PrimitiveType.Sphere,
                new Vector3(-0.43f, 0.66f, -0.28f),
                new Vector3(0.18f, 0.18f, 0.18f), red);
            Part("Headband Tail", PrimitiveType.Cube,
                new Vector3(-0.56f, 0.48f, -0.31f),
                new Vector3(0.12f, 0.48f, 0.08f), red,
                false, new Vector3(0f, 0f, -24f));
        }

        private void BuildGlasses()
        {
            Color lens = new(0.16f, 0.62f, 1f);
            Color gold = new(0.95f, 0.58f, 0.10f);
            Part("Left Lens", PrimitiveType.Sphere,
                new Vector3(-0.22f, 0.54f, 0.48f),
                new Vector3(0.30f, 0.27f, 0.055f), lens, true);
            Part("Right Lens", PrimitiveType.Sphere,
                new Vector3(0.22f, 0.54f, 0.48f),
                new Vector3(0.30f, 0.27f, 0.055f), lens, true);
            Part("Glasses Bridge", PrimitiveType.Cube,
                new Vector3(0f, 0.54f, 0.48f),
                new Vector3(0.18f, 0.035f, 0.035f), gold);
        }

        private void BuildCollar()
        {
            Color blue = new(0.04f, 0.26f, 0.74f);
            for (int index = 0; index < 10; index++)
            {
                float angle = index / 10f * Mathf.PI * 2f;
                Part("Collar Bead", PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(angle) * 0.35f, 0.12f,
                        Mathf.Sin(angle) * 0.35f),
                    new Vector3(0.14f, 0.12f, 0.14f), blue);
            }
            Part("Golden Bell", PrimitiveType.Sphere,
                new Vector3(0f, -0.08f, 0.39f),
                new Vector3(0.24f, 0.24f, 0.18f),
                new Color(1f, 0.62f, 0.08f), true);
        }

        private void BuildFishBackpack()
        {
            Color white = new(0.92f, 0.82f, 0.62f);
            Color red = new(0.72f, 0.055f, 0.035f);
            Part("Fish Backpack", PrimitiveType.Sphere,
                new Vector3(0f, 0.05f, -0.52f),
                new Vector3(0.58f, 0.82f, 0.30f), white,
                false, new Vector3(0f, 0f, 12f));
            Part("Fish Patch", PrimitiveType.Sphere,
                new Vector3(0f, 0.12f, -0.70f),
                new Vector3(0.30f, 0.42f, 0.06f), red);
            Part("Fish Tail", PrimitiveType.Cube,
                new Vector3(0f, -0.52f, -0.50f),
                new Vector3(0.48f, 0.42f, 0.16f), red,
                false, new Vector3(0f, 0f, 45f));
        }

        private void BuildAmulet()
        {
            Color jade = new(0.06f, 0.58f, 0.28f);
            Color gold = new(1f, 0.62f, 0.08f);
            Part("Protection Amulet", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0.46f),
                new Vector3(0.27f, 0.055f, 0.27f), jade, true,
                new Vector3(90f, 0f, 0f));
            Part("Amulet Paw", PrimitiveType.Sphere,
                new Vector3(0f, 0.05f, 0.53f),
                new Vector3(0.11f, 0.11f, 0.035f), gold, true);
        }

        private void BuildWings()
        {
            Color glow = new(0.18f, 0.68f, 1f);
            for (int side = -1; side <= 1; side += 2)
            for (int feather = 0; feather < 3; feather++)
            {
                Part("Spirit Wing", PrimitiveType.Capsule,
                    new Vector3(side * (0.53f + feather * 0.16f),
                        0.13f - feather * 0.12f, -0.42f),
                    new Vector3(0.16f, 0.52f - feather * 0.08f, 0.09f),
                    glow, true,
                    new Vector3(0f, 0f, side * (35f + feather * 9f)));
            }
        }

        private void BuildMagicSteps()
        {
            Color gold = new(1f, 0.56f, 0.05f);
            Part("Left Magic Step", PrimitiveType.Cylinder,
                new Vector3(-0.27f, -0.96f, 0f),
                new Vector3(0.40f, 0.025f, 0.52f), gold, true);
            Part("Right Magic Step", PrimitiveType.Cylinder,
                new Vector3(0.27f, -0.95f, 0.12f),
                new Vector3(0.40f, 0.025f, 0.52f), gold, true);
        }

        private GameObject Part(
            string label,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool emissive = false,
            Vector3 rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = label;
            part.transform.SetParent(accessoryRoot, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation);
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial =
                CreateMaterial(label, color, emissive);
            return part;
        }

        private Material CreateMaterial(string label, Color color, bool emissive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            shader ??= Shader.Find("Sprites/Default");
            Material material = new(shader)
            {
                name = $"Kin Accessory {label}",
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
            }
            ownedMaterials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
            {
                if (material != null) Destroy(material);
            }
            ownedMaterials.Clear();
            palette.Clear();
        }
    }
}
