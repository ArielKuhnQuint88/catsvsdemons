using System.Collections.Generic;
using CatsVsDemons.Economy;
using UnityEngine;

namespace CatsVsDemons.House
{
    internal enum HouseShopCategory
    {
        Clothes,
        Accessories,
        Upgrades
    }

    internal enum HouseShopStat
    {
        None,
        MaximumHealth,
        MaximumEnergy,
        AttackDamage,
        SpecialDamage
    }

    internal sealed class HouseShopItem
    {
        public HouseShopItem(
            string id,
            string name,
            string description,
            HouseShopCategory category,
            int price,
            Rect artworkRegion,
            HouseShopStat stat = HouseShopStat.None,
            int amount = 0)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Price = price;
            ArtworkRegion = artworkRegion;
            Stat = stat;
            Amount = amount;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public HouseShopCategory Category { get; }
        public int Price { get; }
        public Rect ArtworkRegion { get; }
        public HouseShopStat Stat { get; }
        public int Amount { get; }
        public bool IsCosmetic => Category != HouseShopCategory.Upgrades;
    }

    internal static class HouseShopCatalog
    {
        private static readonly HouseShopItem[] Clothes =
        {
            new("samurai_vermelho", "Samurai Vermelho",
                "O traje clássico de Kin. Equilibrado, honrado e cheio de estilo.",
                HouseShopCategory.Clothes, 0,
                new Rect(0.055f, 0.50f, 0.275f, 0.405f)),
            new("ninja_meia_noite", "Ninja da Meia-Noite",
                "Traje furtivo em azul e violeta para patrulhar sem fazer miau.",
                HouseShopCategory.Clothes, 120,
                new Rect(0.362f, 0.50f, 0.275f, 0.405f)),
            new("guardiao_bonsai", "Guardião do Bonsai",
                "Vestes verdes inspiradas na cura e na proteção do jardim.",
                HouseShopCategory.Clothes, 150,
                new Rect(0.670f, 0.50f, 0.275f, 0.405f)),
            new("mestre_lanternas", "Mestre das Lanternas",
                "Armadura cerimonial iluminada pelo fogo das lanternas.",
                HouseShopCategory.Clothes, 180,
                new Rect(0.055f, 0.055f, 0.275f, 0.405f)),
            new("gato_domestico", "Gato Doméstico",
                "Roupa confortável para quem derrotou demônios e merece um sofá.",
                HouseShopCategory.Clothes, 100,
                new Rect(0.362f, 0.055f, 0.275f, 0.405f)),
            new("ronin_espiritual", "Ronin Espiritual",
                "Traje lendário envolvido por energia ancestral azul.",
                HouseShopCategory.Clothes, 220,
                new Rect(0.670f, 0.055f, 0.275f, 0.405f))
        };

        private static readonly HouseShopItem[] Accessories =
        {
            new("faixa_samurai", "Faixa Samurai",
                "Faixa vermelha com o símbolo da pata dourada.",
                HouseShopCategory.Accessories, 35,
                new Rect(0.045f, 0.505f, 0.215f, 0.365f)),
            new("oculos", "Óculos",
                "Lentes azuis para analisar demônios com elegância.",
                HouseShopCategory.Accessories, 45,
                new Rect(0.275f, 0.505f, 0.215f, 0.365f)),
            new("coleira_sino", "Coleira com Sino",
                "Coleira azul com sino dourado. Furtividade: discutível.",
                HouseShopCategory.Accessories, 55,
                new Rect(0.505f, 0.505f, 0.215f, 0.365f)),
            new("mochila_peixe", "Mochila de Peixe",
                "Leva petiscos, poções e prioridades felinas.",
                HouseShopCategory.Accessories, 75,
                new Rect(0.735f, 0.505f, 0.215f, 0.365f)),
            new("amuleto_protecao", "Amuleto de Proteção",
                "Amuleto de jade para afastar a energia demoníaca.",
                HouseShopCategory.Accessories, 110,
                new Rect(0.145f, 0.055f, 0.225f, 0.405f)),
            new("asas_espirituais", "Asas Espirituais",
                "Asas de energia azul que acompanham os movimentos de Kin.",
                HouseShopCategory.Accessories, 180,
                new Rect(0.390f, 0.055f, 0.225f, 0.405f)),
            new("passos_magicos", "Passos Mágicos",
                "Pegadas luminosas de folhas e energia dourada.",
                HouseShopCategory.Accessories, 160,
                new Rect(0.635f, 0.055f, 0.225f, 0.405f))
        };

        private static readonly HouseShopItem[] Upgrades =
        {
            new("coracao_forte", "Coração Forte",
                "+20 de vida máxima para Kin.",
                HouseShopCategory.Upgrades, 60,
                new Rect(0.035f, 0.535f, 0.29f, 0.40f),
                HouseShopStat.MaximumHealth, 20),
            new("armadura_ancestral", "Armadura Ancestral",
                "+35 de vida máxima. Acumula com Coração Forte.",
                HouseShopCategory.Upgrades, 130,
                new Rect(0.355f, 0.535f, 0.29f, 0.40f),
                HouseShopStat.MaximumHealth, 35),
            new("folego_samurai", "Fôlego Samurai",
                "+20 de energia máxima para o golpe especial.",
                HouseShopCategory.Upgrades, 70,
                new Rect(0.675f, 0.535f, 0.29f, 0.40f),
                HouseShopStat.MaximumEnergy, 20),
            new("espirito_cheio", "Espírito Cheio",
                "+35 de energia máxima. Acumula com Fôlego Samurai.",
                HouseShopCategory.Upgrades, 140,
                new Rect(0.035f, 0.065f, 0.29f, 0.40f),
                HouseShopStat.MaximumEnergy, 35),
            new("lamina_afiada", "Lâmina Afiada",
                "+4 de dano em cada ataque normal.",
                HouseShopCategory.Upgrades, 90,
                new Rect(0.355f, 0.065f, 0.29f, 0.40f),
                HouseShopStat.AttackDamage, 4),
            new("golpe_espiritual", "Golpe Espiritual",
                "+15 de dano no golpe de energia em área.",
                HouseShopCategory.Upgrades, 120,
                new Rect(0.675f, 0.065f, 0.29f, 0.40f),
                HouseShopStat.SpecialDamage, 15)
        };

        public static IReadOnlyList<HouseShopItem> Get(
            HouseShopCategory category)
        {
            return category switch
            {
                HouseShopCategory.Clothes => Clothes,
                HouseShopCategory.Accessories => Accessories,
                _ => Upgrades
            };
        }

        public static int GetPurchasedBonus(HouseShopStat stat)
        {
            int total = 0;
            foreach (HouseShopItem item in Upgrades)
            {
                if (item.Stat == stat && HouseShopSave.IsOwned(item))
                {
                    total += item.Amount;
                }
            }
            return total;
        }
    }

    internal static class HouseShopSave
    {
        private const string Prefix = "CatsVsDemons.HouseShop.";
        private const string DefaultOutfit = "samurai_vermelho";

        public static bool IsOwned(HouseShopItem item)
        {
            return item.Price == 0 ||
                PlayerPrefs.GetInt(Prefix + "Owned." + item.Id, 0) == 1;
        }

        public static bool IsEquipped(HouseShopItem item)
        {
            if (!item.IsCosmetic)
            {
                return false;
            }

            return GetEquipped(item.Category) == item.Id;
        }

        public static string GetEquipped(HouseShopCategory category)
        {
            return category switch
            {
                HouseShopCategory.Clothes => PlayerPrefs.GetString(
                    Prefix + "Equipped.Clothes", DefaultOutfit),
                HouseShopCategory.Accessories => PlayerPrefs.GetString(
                    Prefix + "Equipped.Accessories", string.Empty),
                _ => string.Empty
            };
        }

        public static bool TryPurchase(
            HouseShopItem item,
            Wallet wallet,
            out string result)
        {
            if (IsOwned(item))
            {
                result = "Este item já pertence ao Kin.";
                return false;
            }

            if (wallet == null || !wallet.TrySpend(item.Price))
            {
                result = $"Faltam moedas para comprar {item.Name}.";
                return false;
            }

            PlayerPrefs.SetInt(Prefix + "Owned." + item.Id, 1);
            if (item.IsCosmetic)
            {
                SetEquipped(item);
                result = $"{item.Name} comprado e equipado!";
            }
            else
            {
                result = $"{item.Name} comprado e aplicado!";
            }
            PlayerPrefs.Save();
            return true;
        }

        public static bool TryEquip(HouseShopItem item, out string result)
        {
            if (!item.IsCosmetic || !IsOwned(item))
            {
                result = "Compre o item antes de equipá-lo.";
                return false;
            }

            if (IsEquipped(item))
            {
                result = $"{item.Name} já está equipado.";
                return false;
            }

            SetEquipped(item);
            PlayerPrefs.Save();
            result = $"{item.Name} equipado!";
            return true;
        }

        private static void SetEquipped(HouseShopItem item)
        {
            string key = item.Category == HouseShopCategory.Clothes
                ? "Equipped.Clothes"
                : "Equipped.Accessories";
            PlayerPrefs.SetString(Prefix + key, item.Id);
        }
    }
}
