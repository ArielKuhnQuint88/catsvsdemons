using System.Collections.Generic;
using CatsVsDemons.Visuals;
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
            public bool WholeModel;
        }

        private readonly List<PaletteEntry> palette = new();
        private readonly List<Material> ownedMaterials = new();
        private KinEquipmentView equipment;
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
                equipment != null && equipment.HasEquipment)
            {
                return;
            }

            EnsurePaletteMaterials();
            ApplyOutfitPalette(outfitId);
            if (equipment == null) equipment = GetComponent<KinEquipmentView>();
            if (equipment == null) equipment = gameObject.AddComponent<KinEquipmentView>();
            if (!equipment.Apply(outfitId, accessoryId)) return;
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
                        Slot = DetectSlot(original),
                        WholeModel = original.name == "Runtime_Models_Kin"
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
                out Color dark, out _);
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
                if (entry.WholeModel)
                {
                    // A missing imported palette must not repaint Kin's head as clothing.
                    SetMaterialColors(entry.Material, secondary, true);
                    entry.Material.SetColor("_SecondaryColor", fur);
                    entry.Material.SetColor("_AccentColor", primary);
                }
                else SetMaterialColors(entry.Material, color, false);
            }
        }

        private static void GetPalette(string outfitId, out Color primary, out Color secondary,
            out Color dark, out Color accent) => KinEquipmentModels.GetPalette(outfitId,
                out primary, out secondary, out dark, out accent);

        private static void SetMaterialColors(
            Material material,
            Color baseColor,
            bool wholeModel)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);
            if (material.HasProperty("_SecondaryColor"))
                material.SetColor("_SecondaryColor", baseColor);
            if (material.HasProperty("_AccentColor"))
                material.SetColor("_AccentColor", baseColor);
            if (material.HasProperty("_RimColor"))
                material.SetColor("_RimColor",
                    Color.Lerp(baseColor, Color.white, 0.35f));
            if (material.HasProperty("_UseHeightPalette"))
                material.SetFloat("_UseHeightPalette", wholeModel ? 1f : 0f);
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
