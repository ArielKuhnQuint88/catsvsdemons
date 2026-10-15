using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    /// <summary>Wearable geometry adapted directly from HouseShopClothes/HouseShopAccessories artwork.</summary>
    public static class KinEquipmentModels
    {
        private static Color C(uint value) => new(((value >> 16) & 255) / 255f,
            ((value >> 8) & 255) / 255f, (value & 255) / 255f);
        private static readonly Color Gold = C(0xDDAA42), Ivory = C(0xEFE3BC), Red = C(0xA82520),
            Jade = C(0x236C42), Blue = C(0x20569B), Cyan = C(0x65C7F0), Ink = C(0x172133);

        public static void GetPalette(string id, out Color primary, out Color secondary, out Color dark, out Color accent)
        {
            primary = Red; secondary = Ivory; dark = C(0x332521); accent = Gold;
            switch (id)
            {
                case "ninja_meia_noite": primary = C(0x20233F); secondary = C(0x543478); dark = C(0x101525); accent = C(0xC0A34A); break;
                case "guardiao_bonsai": primary = C(0x1F5338); secondary = C(0xE4DEC1); dark = C(0x183827); accent = Gold; break;
                case "mestre_lanternas": primary = C(0x932C1C); secondary = C(0x2C211F); dark = C(0x161A20); accent = C(0xEEAE37); break;
                case "gato_domestico": primary = C(0xEBDEC1); secondary = C(0x285889); dark = C(0x163452); accent = Red; break;
                case "ronin_espiritual": primary = C(0x14223C); secondary = C(0x23517C); dark = C(0x101A2B); accent = C(0xB9C8D7); break;
            }
        }

        private static void Add(List<KinEquipmentPart> parts, string name, KinEquipmentAnchor anchor,
            KinEquipmentGeometry geometry, float sway = 0, Vector3 pivot = default, bool glass = false)
        {
            if (geometry.VertexCount == 0) return;
            parts.Add(new KinEquipmentPart { Name = name, Anchor = anchor, Mesh = geometry.Build(name),
                Sway = sway, Pivot = pivot, Glass = glass });
        }

        public static List<KinEquipmentPart> BuildOutfit(string id)
        {
            List<KinEquipmentPart> parts = new();
            GetPalette(id, out Color primary, out Color secondary, out Color dark, out Color accent);
            KinEquipmentGeometry torso = new(), belt = new();
            bool domestic = id == "gato_domestico", ninja = id == "ninja_meia_noite";
            torso.Shell(Vector3.zero, new[] { 0.247f, 0.34f, 0.437f },
                new[] { new Vector2(0.199f, 0.17f), new Vector2(0.175f, 0.164f), new Vector2(0.151f, 0.144f) }, primary,
                domestic ? 36f : 14f, domestic ? 324f : 346f);
            // Crossed lapels, stitched piping and a raised knot replace a flat color swap.
            torso.Panel(new[] { new Vector3(-0.11f, 0.438f, 0.11f), new Vector3(-0.035f, 0.345f, 0.178f),
                new Vector3(0.055f, 0.258f, 0.174f) }, new[] { 0.038f, 0.041f, 0.032f }, secondary);
            torso.Panel(new[] { new Vector3(0.11f, 0.438f, 0.11f), new Vector3(0.04f, 0.364f, 0.171f),
                new Vector3(-0.03f, 0.285f, 0.18f) }, new[] { 0.025f, 0.025f, 0.025f }, domestic ? Ivory : secondary);
            torso.Tube(new[] { new Vector3(-0.119f, 0.437f, 0.122f), new Vector3(-0.052f, 0.348f, 0.182f),
                new Vector3(0.037f, 0.265f, 0.184f) }, 0.003f, domestic ? secondary : accent, 0.6f);
            belt.Band(new Vector3(0, 0.266f, 0), 0.213f, 0.177f, 0.04f, ninja ? secondary : domestic ? Blue : primary);
            belt.Ring(new Vector3(0, 0.27f, 0), 0.219f, 0.183f, 0.007f, id == "guardiao_bonsai" ? C(0x668237) : primary, 0, 0, true);
            belt.Ellipsoid(new Vector3(0, 0.263f, 0.188f), new Vector3(0.021f, 0.022f, 0.012f), primary);
            for (int side = -1; side <= 1; side += 2)
            {
                belt.Ring(new Vector3(side * 0.024f, 0.263f, 0.187f), 0.023f, 0.016f, 0.004f, primary);
                if (!domestic) belt.Tassel(new Vector3(side * 0.033f, 0.245f, 0.19f), 0.061f, primary, Gold);
                KinEquipmentGeometry sleeve = new(), leg = new(), boot = new();
                sleeve.Sleeve(new Vector3(side * 0.15f, 0.406f, 0), new Vector3(side * 0.252f, 0.302f, 0),
                    0.062f, domestic ? 0.076f : ninja ? 0.056f : 0.09f, primary);
                sleeve.Tube(new[] { new Vector3(side * 0.235f, 0.325f, 0.074f), new Vector3(side * 0.265f, 0.294f, 0.071f) },
                    0.0035f, domestic ? Blue : secondary);
                leg.Ellipsoid(new Vector3(side * 0.117f, 0.158f, 0), new Vector3(0.098f, 0.09f, 0.098f), domestic ? secondary : dark);
                boot.Band(new Vector3(side * 0.122f, 0.082f, 0), 0.075f, 0.079f, 0.069f, domestic ? Ivory : primary);
                boot.Ring(new Vector3(side * 0.122f, 0.113f, 0), 0.077f, 0.081f, 0.004f, domestic ? Blue : accent, 0.45f, 0, true);
                boot.Tube(new[] { new Vector3(side * 0.175f, 0.09f, 0.067f), new Vector3(side * 0.10f, 0.052f, 0.079f) },
                    0.005f, domestic ? Ivory : secondary);
                if (!domestic && !ninja)
                {
                    sleeve.Ellipsoid(new Vector3(side * 0.177f, 0.407f, 0.008f), new Vector3(0.085f, 0.035f, 0.09f), primary, 0.45f);
                    sleeve.Ring(new Vector3(side * 0.177f, 0.423f, 0.01f), 0.074f, 0.078f, 0.004f, accent, 0.8f, 0, true);
                    sleeve.Paw(new Vector3(side * 0.228f, 0.358f, 0.085f), 0.041f, accent);
                }
                if (domestic)
                {
                    sleeve.Band(new Vector3(side * 0.263f, 0.29f, 0), 0.067f, 0.069f, 0.025f, Blue);
                    sleeve.Paw(new Vector3(side * 0.216f, 0.366f, 0.077f), 0.035f, Blue);
                    FishSlipper(boot, new Vector3(side * 0.123f, 0.026f, 0.054f));
                }
                else if (ninja)
                {
                    sleeve.Tube(new[] { new Vector3(side * 0.175f, 0.394f, 0.054f), new Vector3(side * 0.251f, 0.309f, 0.056f) }, 0.004f, Cyan, 0.2f, 0.15f);
                    boot.Band(new Vector3(side * 0.122f, 0.068f, 0), 0.079f, 0.083f, 0.015f, secondary);
                    boot.Paw(new Vector3(side * 0.122f, 0.087f, 0.084f), 0.024f, Gold);
                }
                else if (id == "guardiao_bonsai")
                {
                    for (int leaf = 0; leaf < 3; leaf++)
                        sleeve.Leaf(new Vector3(side * 0.153f, 0.415f, 0.035f),
                            new Vector3(side * (0.21f + leaf * 0.023f), 0.379f - leaf * 0.007f, 0.08f), 0.02f, Jade);
                    boot.Band(new Vector3(side * 0.122f, 0.065f, 0), 0.077f, 0.081f, 0.016f, Ivory);
                }
                else if (id == "ronin_espiritual")
                {
                    sleeve.Tube(new[] { new Vector3(side * 0.15f, 0.415f, 0.075f), new Vector3(side * 0.25f, 0.304f, 0.075f) }, 0.004f, Cyan, 0.5f, 0.35f);
                    sleeve.Ellipsoid(new Vector3(side * 0.18f, 0.423f, 0.032f), new Vector3(0.055f, 0.022f, 0.055f), accent, 0.8f);
                }
                Add(parts, id + " sculpted sleeve " + side, side < 0 ? KinEquipmentAnchor.LeftArm : KinEquipmentAnchor.RightArm, sleeve);
                Add(parts, id + " tailored leg " + side, side < 0 ? KinEquipmentAnchor.LeftLeg : KinEquipmentAnchor.RightLeg, leg);
                Add(parts, id + " detailed boot " + side, side < 0 ? KinEquipmentAnchor.LeftFoot : KinEquipmentAnchor.RightFoot, boot);
            }
            if (domestic) Domestic(torso, belt);
            else if (ninja) Ninja(torso, belt, parts);
            else if (id == "guardiao_bonsai") Guardian(torso, belt, parts);
            else if (id == "mestre_lanternas") LanternMaster(torso, belt, parts);
            else if (id == "ronin_espiritual") Ronin(torso, belt, parts);
            else Samurai(torso, belt, parts);
            Add(parts, id + " embroidered torso", KinEquipmentAnchor.Chest, torso);
            Add(parts, id + " braided obi and ornaments", KinEquipmentAnchor.Pelvis, belt);
            return parts;
        }

        private static void Samurai(KinEquipmentGeometry chest, KinEquipmentGeometry belt, List<KinEquipmentPart> parts)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                KinEquipmentGeometry skirt = new();
                for (int row = 0; row < 3; row++)
                {
                    float y = 0.245f - row * 0.034f;
                    skirt.Panel(new[] { new Vector3(side * 0.153f, y, 0.095f), new Vector3(side * 0.179f, y - 0.029f, 0.097f) }, new[] { 0.065f, 0.069f }, Red, 0.007f, 0.45f);
                    skirt.Tube(new[] { new Vector3(side * 0.147f - 0.03f, y - 0.028f, 0.107f), new Vector3(side * 0.147f + 0.03f, y - 0.028f, 0.107f) }, 0.003f, Gold, 0.8f);
                    for (int rivet = -1; rivet <= 1; rivet += 2)
                        skirt.Ellipsoid(new Vector3(side * 0.15f + rivet * 0.026f, y - 0.008f, 0.107f), new Vector3(0.004f, 0.004f, 0.002f), Gold, 1, 0, 6, 3);
                }
                Add(parts, "samurai_vermelho lacquered armor plates " + side, KinEquipmentAnchor.Pelvis, skirt, 0.25f, new Vector3(side * 0.14f, 0.26f, 0.08f));
            }
            KinEquipmentGeometry apron = new();
            apron.Panel(new[] { new Vector3(0.045f, 0.25f, 0.185f), new Vector3(0.028f, 0.19f, 0.163f), new Vector3(0.014f, 0.12f, 0.124f) }, new[] { 0.063f, 0.063f, 0.072f }, Ivory);
            Waves(apron, new Vector3(0.018f, 0.152f, 0.145f), 0.028f, Gold);
            Add(parts, "samurai_vermelho wave embroidered front cloth", KinEquipmentAnchor.Pelvis, apron, 0.35f, new Vector3(0.045f, 0.25f, 0.185f));
            Flowers(chest, new Vector3(-0.09f, 0.367f, 0.153f), Ivory);
            Flowers(chest, new Vector3(0.085f, 0.307f, 0.163f), Gold);
            belt.Paw(new Vector3(0.085f, 0.263f, 0.17f), 0.032f, Gold);
        }

        private static void Ninja(KinEquipmentGeometry chest, KinEquipmentGeometry belt, List<KinEquipmentPart> parts)
        {
            chest.Band(new Vector3(0, 0.437f, 0), 0.14f, 0.142f, 0.035f, Ink);
            KinEquipmentGeometry scarf = new();
            scarf.Panel(new[] { new Vector3(-0.11f, 0.422f, -0.115f), new Vector3(-0.185f, 0.34f, -0.185f),
                new Vector3(-0.22f, 0.22f, -0.19f) }, new[] { 0.047f, 0.052f, 0.033f }, C(0x543478));
            scarf.Tube(new[] { new Vector3(-0.11f, 0.422f, -0.12f), new Vector3(-0.205f, 0.34f, -0.192f), new Vector3(-0.236f, 0.22f, -0.192f) }, 0.003f, Blue, 0.2f, 0.25f);
            Add(parts, "ninja_meia_noite long violet scarf", KinEquipmentAnchor.Chest, scarf, 0.9f, new Vector3(-0.11f, 0.422f, -0.115f));
            belt.Paw(new Vector3(-0.065f, 0.248f, 0.177f), 0.032f, Gold);
            chest.Paw(new Vector3(0.104f, 0.392f, 0.125f), 0.023f, Gold);
            KinEquipmentGeometry front = new();
            front.Panel(new[] { new Vector3(-0.07f, 0.246f, 0.186f), new Vector3(-0.075f, 0.115f, 0.132f) }, new[] { 0.055f, 0.07f }, C(0x34204B));
            front.Paw(new Vector3(-0.075f, 0.178f, 0.168f), 0.028f, C(0x8971B4));
            Add(parts, "ninja_meia_noite sash with paw crest", KinEquipmentAnchor.Pelvis, front, 0.35f, new Vector3(-0.07f, 0.246f, 0.186f));
        }

        private static void Guardian(KinEquipmentGeometry chest, KinEquipmentGeometry belt, List<KinEquipmentPart> parts)
        {
            chest.Shell(Vector3.zero, new[] { 0.277f, 0.37f, 0.437f }, new[] { new Vector2(0.207f, 0.178f), new Vector2(0.177f, 0.173f), new Vector2(0.156f, 0.147f) }, Ivory, 55, 305);
            for (int side = -1; side <= 1; side += 2)
            {
                KinEquipmentGeometry cloth = new();
                cloth.Panel(new[] { new Vector3(side * 0.09f, 0.254f, 0.186f), new Vector3(side * 0.12f, 0.164f, 0.152f), new Vector3(side * 0.154f, 0.098f, 0.124f) }, new[] { 0.071f, 0.072f, 0.076f }, side < 0 ? Ivory : Jade);
                cloth.Tube(new[] { new Vector3(side * 0.124f, 0.25f, 0.193f), new Vector3(side * 0.155f, 0.164f, 0.16f), new Vector3(side * 0.19f, 0.10f, 0.13f) }, 0.003f, Gold, 0.7f);
                for (int leaf = 0; leaf < 4; leaf++)
                    cloth.Leaf(new Vector3(side * 0.104f, 0.205f - leaf * 0.022f, 0.173f - leaf * 0.014f), new Vector3(side * 0.13f, 0.218f - leaf * 0.022f, 0.174f - leaf * 0.014f), 0.006f, side < 0 ? Jade : Ivory);
                Add(parts, "guardiao_bonsai leaf embroidered robe " + side, KinEquipmentAnchor.Pelvis, cloth, 0.4f, new Vector3(side * 0.09f, 0.254f, 0.186f));
            }
            belt.Ring(new Vector3(0, 0.23f, 0.188f), 0.031f, 0.031f, 0.004f, Gold, 0.85f);
            belt.Leaf(new Vector3(0, 0.218f, 0.19f), new Vector3(0.014f, 0.25f, 0.19f), 0.011f, Jade);
            belt.Ellipsoid(new Vector3(0, 0.188f, 0.174f), new Vector3(0.01f, 0.014f, 0.009f), Jade, 0.4f);
            chest.Tube(new[] { new Vector3(-0.085f, 0.402f, 0.133f), new Vector3(-0.06f, 0.36f, 0.177f) }, 0.003f, Gold, 0.8f);
        }

        private static void LanternMaster(KinEquipmentGeometry chest, KinEquipmentGeometry belt, List<KinEquipmentPart> parts)
        {
            KinEquipmentGeometry hat = new();
            const int sides = 32;
            Vector3 peak = new(0, 1.015f, -0.025f);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p = new(Mathf.Cos(a) * 0.365f, 0.871f, Mathf.Sin(a) * 0.25f - 0.025f);
                Vector3 q = new(Mathf.Cos(b) * 0.365f, 0.871f, Mathf.Sin(b) * 0.25f - 0.025f);
                hat.Triangle(p, peak, q, C(0x2A2521), 0.45f, 0, true);
                if (i % 4 == 0) hat.Tube(new[] { p, peak }, 0.003f, Gold, 0.85f);
            }
            hat.Ring(new Vector3(0, 0.871f, -0.025f), 0.367f, 0.252f, 0.006f, Gold, 0.8f, 0, true, 32);
            hat.Paw(new Vector3(0, 0.929f, 0.159f), 0.054f, Gold, 0.3f);
            for (int side = -1; side <= 1; side += 2)
            {
                Lantern(hat, new Vector3(side * 0.30f, 0.83f, 0.025f), 0.021f);
                hat.Tassel(new Vector3(side * 0.326f, 0.866f, 0.092f), 0.095f, Red, Gold);
                Lantern(chest, new Vector3(side * 0.152f, 0.42f, 0.075f), 0.023f);
                KinEquipmentGeometry skirt = new();
                skirt.Panel(new[] { new Vector3(side * 0.123f, 0.258f, 0.164f), new Vector3(side * 0.15f, 0.174f, 0.149f), new Vector3(side * 0.19f, 0.093f, 0.096f) }, new[] { 0.071f, 0.094f, 0.076f }, Ink);
                for (int flame = 0; flame < 3; flame++)
                    skirt.Leaf(new Vector3(side * 0.156f + (flame - 1) * 0.025f, 0.114f, 0.126f), new Vector3(side * 0.147f + (flame - 1) * 0.024f, 0.197f + flame % 2 * 0.016f, 0.16f), 0.012f, C(0xEBA135), 0.15f);
                Add(parts, "mestre_lanternas flame embroidered panels " + side, KinEquipmentAnchor.Pelvis, skirt, 0.4f, new Vector3(side * 0.123f, 0.258f, 0.164f));
            }
            Add(parts, "mestre_lanternas ceremonial lantern hat", KinEquipmentAnchor.Head, hat);
            Lantern(belt, new Vector3(0.075f, 0.22f, 0.18f), 0.021f);
        }

        private static void Domestic(KinEquipmentGeometry chest, KinEquipmentGeometry belt)
        {
            for (int i = 0; i < 3; i++)
                chest.Ellipsoid(new Vector3(0.047f, 0.39f - i * 0.045f, 0.177f), new Vector3(0.006f, 0.006f, 0.003f), Gold, 0.7f, 0, 8, 4);
            chest.Paw(new Vector3(-0.11f, 0.32f, 0.147f), 0.046f, Blue);
            chest.Panel(new[] { new Vector3(-0.12f, 0.30f, 0.155f), new Vector3(-0.12f, 0.26f, 0.166f) }, new[] { 0.059f, 0.06f }, Ivory);
            chest.Tube(new[] { new Vector3(-0.15f, 0.298f, 0.163f), new Vector3(-0.09f, 0.298f, 0.163f) }, 0.003f, Blue);
            chest.Band(new Vector3(0, 0.444f, 0), 0.141f, 0.144f, 0.022f, Red);
            chest.Panel(new[] { new Vector3(0.03f, 0.435f, 0.154f), new Vector3(0.012f, 0.36f, 0.182f) }, new[] { 0.047f, 0.025f }, Red);
            chest.Paw(new Vector3(0.018f, 0.404f, 0.17f), 0.023f, Ivory);
            belt.Tassel(new Vector3(0.087f, 0.239f, 0.161f), 0.058f, Red, Gold);
        }

        private static void Ronin(KinEquipmentGeometry chest, KinEquipmentGeometry belt, List<KinEquipmentPart> parts)
        {
            chest.Ellipsoid(new Vector3(0, 0.415f, 0.149f), new Vector3(0.018f, 0.025f, 0.009f), Cyan, 0.75f, 0.6f);
            chest.Ring(new Vector3(0, 0.415f, 0.155f), 0.022f, 0.029f, 0.003f, Gold, 0.8f);
            for (int side = -1; side <= 1; side += 2)
            {
                KinEquipmentGeometry cape = new();
                cape.Panel(new[] { new Vector3(side * 0.15f, 0.42f, -0.144f), new Vector3(side * 0.225f, 0.31f, -0.195f),
                    new Vector3(side * 0.253f, 0.19f, -0.224f), new Vector3(side * 0.24f, 0.053f, -0.23f) },
                    new[] { 0.12f, 0.16f, 0.20f, 0.15f }, C(0x142541), 0.017f);
                for (int ribbon = 0; ribbon < 3; ribbon++)
                    cape.Leaf(new Vector3(side * (0.2f + ribbon * 0.035f), 0.26f, -0.203f),
                        new Vector3(side * (0.27f + ribbon * 0.06f), 0.105f - ribbon * 0.025f, -0.18f), 0.023f, Cyan, 0.3f);
                cape.Tube(new[] { new Vector3(side * 0.209f, 0.42f, -0.14f), new Vector3(side * 0.31f, 0.31f, -0.198f),
                    new Vector3(side * 0.346f, 0.19f, -0.23f), new Vector3(side * 0.312f, 0.06f, -0.23f) }, 0.003f, Cyan, 0.45f, 0.45f);
                Add(parts, "ronin_espiritual layered spirit cloak " + side, KinEquipmentAnchor.Chest, cape, 0.7f, new Vector3(side * 0.15f, 0.42f, -0.144f));
            }
            belt.Ring(new Vector3(0.02f, 0.25f, 0.19f), 0.022f, 0.025f, 0.004f, Gold, 0.9f);
            belt.Tassel(new Vector3(-0.034f, 0.24f, 0.19f), 0.086f, Ivory, Gold);
        }

        private static void FishSlipper(KinEquipmentGeometry geo, Vector3 at)
        {
            geo.Ellipsoid(at + Vector3.down * 0.002f, new Vector3(0.088f, 0.019f, 0.11f), Ivory);
            geo.Ellipsoid(at + new Vector3(0, 0.018f, 0.035f), new Vector3(0.078f, 0.035f, 0.08f), Blue);
            for (int side = -1; side <= 1; side += 2)
            {
                geo.Ellipsoid(at + new Vector3(side * 0.053f, 0.03f, 0.097f), new Vector3(0.009f, 0.01f, 0.003f), Ivory, 0.3f);
                geo.Ellipsoid(at + new Vector3(side * 0.053f, 0.03f, 0.1f), new Vector3(0.004f, 0.006f, 0.002f), Ink);
                geo.Leaf(at + new Vector3(side * 0.073f, 0.012f, 0.02f), at + new Vector3(side * 0.099f, 0.023f, -0.005f), 0.013f, Cyan);
            }
        }

        private static void Flowers(KinEquipmentGeometry geo, Vector3 at, Color color)
        {
            for (int f = 0; f < 3; f++)
            {
                Vector3 center = at + new Vector3((f % 2) * 0.025f, -f * 0.025f, f * 0.003f);
                for (int p = 0; p < 5; p++)
                {
                    float a = p * Mathf.PI * 2f / 5;
                    geo.Ellipsoid(center + new Vector3(Mathf.Cos(a) * 0.009f, Mathf.Sin(a) * 0.009f, 0), new Vector3(0.005f, 0.005f, 0.002f), color, 0.3f, 0, 6, 3);
                }
                geo.Ellipsoid(center + Vector3.forward * 0.003f, new Vector3(0.003f, 0.003f, 0.002f), Gold, 0.8f, 0, 6, 3);
            }
        }

        private static void Waves(KinEquipmentGeometry geo, Vector3 at, float size, Color color)
        {
            for (int row = 0; row < 3; row++)
            {
                List<Vector3> curve = new();
                for (int i = 0; i <= 10; i++)
                {
                    float a = i * Mathf.PI / 10;
                    curve.Add(at + new Vector3(Mathf.Cos(a) * size, Mathf.Sin(a) * size * 0.4f + row * 0.009f, -row * 0.004f));
                }
                geo.Tube(curve, 0.0017f, color, 0.5f, 0, 5);
            }
        }

        private static void Lantern(KinEquipmentGeometry geo, Vector3 at, float size)
        {
            geo.Ellipsoid(at, new Vector3(size, size * 1.18f, size), C(0xF6B148), 0.2f, 0.45f);
            geo.Ring(at + Vector3.up * size, size * 0.68f, size * 0.68f, size * 0.14f, Gold, 0.85f, 0, true, 12);
            geo.Ring(at - Vector3.up * size, size * 0.68f, size * 0.68f, size * 0.14f, Gold, 0.85f, 0, true, 12);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                geo.Tube(new[] { at + new Vector3(Mathf.Cos(a) * size, -size * 0.9f, Mathf.Sin(a) * size),
                    at + new Vector3(Mathf.Cos(a) * size, size * 0.9f, Mathf.Sin(a) * size) }, size * 0.07f, Red, 0.3f, 0, 5);
            }
        }

        public static List<KinEquipmentPart> BuildAccessory(string id)
        {
            List<KinEquipmentPart> parts = new(); KinEquipmentGeometry geo = new();
            switch (id)
            {
                case "faixa_samurai": Headband(geo); Add(parts, id + " embroidered silk band", KinEquipmentAnchor.Head, geo); break;
                case "oculos": Glasses(parts); break;
                case "coleira_sino": Collar(geo); Add(parts, id + " blue collar bell and fish charm", KinEquipmentAnchor.Chest, geo); break;
                case "mochila_peixe": Backpack(geo); Add(parts, id + " sculpted koi backpack and straps", KinEquipmentAnchor.Chest, geo); break;
                case "amuleto_protecao": Amulet(geo); Add(parts, id + " braided jade paw medallion", KinEquipmentAnchor.Chest, geo); break;
                case "asas_espirituais": Wings(parts); break;
                // This accessory emits footprints from movement, not solid cylinders under the feet.
                case "passos_magicos": break;
            }
            return parts;
        }

        private static void Headband(KinEquipmentGeometry geo)
        {
            geo.Band(new Vector3(0, 0.806f, -0.012f), 0.275f, 0.22f, 0.049f, Red);
            geo.Ring(new Vector3(0, 0.831f, -0.012f), 0.276f, 0.221f, 0.003f, Gold, 0.8f, 0, true);
            geo.Ring(new Vector3(0, 0.781f, -0.012f), 0.276f, 0.221f, 0.003f, Gold, 0.8f, 0, true);
            geo.Panel(new[] { new Vector3(0, 0.831f, 0.211f), new Vector3(0, 0.781f, 0.211f) }, new[] { 0.097f, 0.097f }, C(0x71211E), 0.003f, 0.3f);
            geo.Paw(new Vector3(0, 0.806f, 0.222f), 0.045f, Gold);
            geo.Ellipsoid(new Vector3(0.275f, 0.802f, -0.035f), new Vector3(0.022f, 0.029f, 0.023f), Red);
            geo.Panel(new[] { new Vector3(0.281f, 0.8f, -0.05f), new Vector3(0.332f, 0.75f, -0.08f), new Vector3(0.36f, 0.68f, -0.07f) }, new[] { 0.026f, 0.043f, 0.031f }, Red);
            geo.Panel(new[] { new Vector3(0.284f, 0.805f, -0.051f), new Vector3(0.343f, 0.814f, -0.063f), new Vector3(0.367f, 0.846f, -0.06f) }, new[] { 0.025f, 0.042f, 0.023f }, Red);
            Waves(geo, new Vector3(0.335f, 0.728f, -0.065f), 0.015f, Gold);
        }

        private static void Glasses(List<KinEquipmentPart> parts)
        {
            KinEquipmentGeometry frame = new(), glass = new();
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 center = new(side * 0.129f, 0.628f, 0.259f);
                frame.Ring(center, 0.082f, 0.077f, 0.0045f, Gold, 0.95f, 0, false, 32);
                frame.Tube(new[] { center + new Vector3(side * 0.083f, 0, 0), new Vector3(side * 0.253f, 0.64f, 0.13f),
                    new Vector3(side * 0.265f, 0.637f, -0.02f) }, 0.006f, Ink, 0.25f);
                frame.Ellipsoid(center + new Vector3(side * 0.083f, 0, 0.002f), new Vector3(0.009f, 0.012f, 0.009f), Gold, 0.9f);
                const int count = 32;
                for (int i = 0; i < count; i++)
                {
                    float a = i * Mathf.PI * 2f / count, b = (i + 1) * Mathf.PI * 2f / count;
                    glass.Triangle(center, center + new Vector3(Mathf.Cos(a) * 0.078f, Mathf.Sin(a) * 0.073f, 0),
                        center + new Vector3(Mathf.Cos(b) * 0.078f, Mathf.Sin(b) * 0.073f, 0), Cyan, 0.8f);
                }
            }
            frame.Tube(new[] { new Vector3(-0.047f, 0.635f, 0.261f), new Vector3(0, 0.661f, 0.268f), new Vector3(0.047f, 0.635f, 0.261f) }, 0.0045f, Gold, 0.9f);
            Add(parts, "oculos round gold frame and temples", KinEquipmentAnchor.Head, frame);
            Add(parts, "oculos transparent blue lenses", KinEquipmentAnchor.Head, glass, glass: true);
        }

        private static void Collar(KinEquipmentGeometry geo)
        {
            geo.Band(new Vector3(0, 0.451f, 0), 0.163f, 0.155f, 0.037f, Blue);
            geo.Ring(new Vector3(0, 0.47f, 0), 0.164f, 0.156f, 0.0025f, Gold, 0.9f, 0, true);
            geo.Ring(new Vector3(0, 0.432f, 0), 0.164f, 0.156f, 0.0025f, Cyan);
            geo.Ring(new Vector3(0, 0.422f, 0.162f), 0.018f, 0.02f, 0.004f, Gold, 0.9f);
            geo.Ellipsoid(new Vector3(0, 0.382f, 0.173f), new Vector3(0.048f, 0.045f, 0.041f), Gold, 0.95f);
            geo.Ring(new Vector3(0, 0.385f, 0.173f), 0.048f, 0.042f, 0.003f, C(0xF9D477), 0.95f, 0, true);
            geo.Ellipsoid(new Vector3(0, 0.383f, 0.214f), new Vector3(0.006f, 0.009f, 0.002f), Ink);
            geo.Tube(new[] { new Vector3(0, 0.384f, 0.216f), new Vector3(0, 0.345f, 0.195f) }, 0.003f, Ink);
            geo.Tassel(new Vector3(-0.042f, 0.427f, 0.169f), 0.075f, Red, Gold);
            geo.Tassel(new Vector3(0.047f, 0.424f, 0.17f), 0.063f, Red, Gold);
            geo.Ellipsoid(new Vector3(0.084f, 0.396f, 0.151f), new Vector3(0.013f, 0.024f, 0.004f), Blue, 0.5f);
            geo.Leaf(new Vector3(0.084f, 0.373f, 0.155f), new Vector3(0.073f, 0.357f, 0.155f), 0.011f, Gold);
        }

        private static void Backpack(KinEquipmentGeometry geo)
        {
            Vector3 at = new(0, 0.308f, -0.212f);
            geo.Ellipsoid(at, new Vector3(0.115f, 0.142f, 0.075f), Ivory, 0.1f, 0, 16, 10);
            geo.Ellipsoid(at + new Vector3(0, 0.105f, -0.006f), new Vector3(0.108f, 0.062f, 0.078f), Red, 0.2f, 0, 16, 8);
            for (int row = 0; row < 4; row++) for (int col = -2; col <= 2; col++)
            {
                Vector3 p = at + new Vector3(col * 0.034f + (row % 2) * 0.011f, 0.05f - row * 0.036f, -0.067f + Mathf.Abs(col) * 0.006f);
                geo.Ellipsoid(p, new Vector3(0.022f, 0.029f, 0.012f), (row + col) % 3 == 0 ? Ivory : Red, 0.15f, 0, 8, 4);
                geo.Ring(p - Vector3.forward * 0.012f, 0.02f, 0.026f, 0.0018f, Ivory, 0.2f, 0, false, 12);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                geo.Tube(new[] { new Vector3(side * 0.087f, 0.403f, -0.19f), new Vector3(side * 0.115f, 0.45f, -0.09f),
                    new Vector3(side * 0.136f, 0.405f, 0.092f), new Vector3(side * 0.144f, 0.26f, 0.136f),
                    new Vector3(side * 0.077f, 0.199f, -0.21f) }, 0.011f, Red, 0, 0, 8);
                geo.Ellipsoid(at + new Vector3(side * 0.095f, 0.11f, -0.047f), new Vector3(0.019f, 0.021f, 0.01f), Gold, 0.9f);
                geo.Ellipsoid(at + new Vector3(side * 0.095f, 0.11f, -0.057f), new Vector3(0.011f, 0.014f, 0.005f), Ink, 0.2f);
                geo.Ellipsoid(at + new Vector3(side * 0.091f, 0.117f, -0.061f), new Vector3(0.003f, 0.004f, 0.002f), Ivory);
                geo.Leaf(at + new Vector3(side * 0.101f, 0.01f, -0.02f), at + new Vector3(side * 0.18f, -0.051f, -0.045f), 0.027f, Ivory);
                geo.Leaf(at + new Vector3(0, -0.124f, -0.01f), at + new Vector3(side * 0.105f, -0.238f, -0.08f), 0.04f, Red);
                geo.Tassel(at + new Vector3(side * 0.043f, 0.06f, -0.085f), 0.047f, Red, Gold);
            }
            geo.Ring(at + new Vector3(0, 0.08f, -0.082f), 0.044f, 0.025f, 0.004f, Ivory, 0.3f);
        }

        private static void Amulet(KinEquipmentGeometry geo)
        {
            List<Vector3> cord = new();
            for (int i = 0; i <= 28; i++)
            {
                float a = i * Mathf.PI * 2f / 28;
                cord.Add(new Vector3(Mathf.Sin(a) * 0.164f, 0.451f - Mathf.Max(0, Mathf.Cos(a)) * 0.049f, Mathf.Cos(a) * 0.161f));
            }
            geo.Tube(cord, 0.005f, Red);
            for (int i = 0; i < 8; i++)
            {
                float a = (i - 3.5f) * 0.2f;
                geo.Ellipsoid(new Vector3(Mathf.Sin(a) * 0.164f, 0.454f - Mathf.Cos(a) * 0.049f, Mathf.Cos(a) * 0.164f), new Vector3(0.009f, 0.012f, 0.009f), i % 3 == 0 ? Jade : Gold, 0.6f, 0, 8, 4);
            }
            geo.Ellipsoid(new Vector3(0, 0.352f, 0.18f), new Vector3(0.054f, 0.054f, 0.009f), Jade, 0.45f, 0.08f);
            geo.Ring(new Vector3(0, 0.352f, 0.189f), 0.056f, 0.056f, 0.004f, Gold, 0.9f);
            geo.Ring(new Vector3(0, 0.352f, 0.191f), 0.046f, 0.046f, 0.0018f, Ivory, 0.8f);
            geo.Paw(new Vector3(0, 0.351f, 0.199f), 0.049f, Gold, 0.18f);
            geo.Ellipsoid(new Vector3(0, 0.292f, 0.189f), new Vector3(0.01f, 0.015f, 0.01f), Jade, 0.6f);
            geo.Tassel(new Vector3(0, 0.275f, 0.187f), 0.076f, Red, Gold);
            geo.Panel(new[] { new Vector3(-0.044f, 0.309f, 0.184f), new Vector3(-0.073f, 0.238f, 0.18f) }, new[] { 0.022f, 0.03f }, Ivory);
            geo.Paw(new Vector3(-0.058f, 0.274f, 0.191f), 0.018f, Gold);
        }

        private static void Wings(List<KinEquipmentPart> parts)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                KinEquipmentGeometry wing = new(); Vector3 pivot = new(side * 0.112f, 0.397f, -0.176f);
                wing.Ellipsoid(pivot, new Vector3(0.025f, 0.037f, 0.024f), Gold, 0.9f);
                wing.Ellipsoid(pivot + new Vector3(0, 0, -0.018f), new Vector3(0.014f, 0.021f, 0.01f), Cyan, 0.7f, 0.45f);
                for (int feather = 0; feather < 7; feather++)
                {
                    Vector3 start = pivot + new Vector3(side * feather * 0.022f, feather * 0.014f, -0.012f);
                    Vector3 tip = new(side * (0.37f + feather * 0.022f), 0.61f - feather * 0.058f, -0.197f - feather * 0.007f);
                    wing.Leaf(start, tip, 0.043f - feather * 0.002f, Color.Lerp(Cyan, Blue, feather / 8f), 0.28f);
                    wing.Tube(new[] { start, Vector3.Lerp(start, tip, 0.53f) - Vector3.forward * 0.01f, tip }, 0.002f, Ivory, 0.4f, 0.28f, 5);
                }
                wing.Tube(new[] { pivot, new Vector3(side * 0.245f, 0.527f, -0.184f), new Vector3(side * 0.371f, 0.63f, -0.181f), new Vector3(side * 0.419f, 0.655f, -0.18f) }, 0.0028f, Cyan, 0.2f, 0.6f);
                wing.Tassel(pivot - Vector3.up * 0.024f, 0.058f, Ivory, Gold);
                Add(parts, "asas_espirituais layered blue feathers " + side, KinEquipmentAnchor.Chest, wing,
                    side * 1.2f, pivot);
            }
        }

        public static Mesh BuildFootprint()
        {
            KinEquipmentGeometry geo = new();
            // Draw in XY, then lay the whole mesh on the ground in the trail component.
            geo.Paw(Vector3.zero, 0.11f, Gold, 0.9f);
            geo.Ring(Vector3.zero, 0.082f, 0.066f, 0.002f, C(0xFFD468), 0.4f, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? 1 : -1;
                geo.Leaf(new Vector3(side * 0.087f, (i - 1) * 0.035f, 0),
                    new Vector3(side * 0.115f, (i - 1) * 0.035f + 0.028f, 0), 0.014f, Red);
                geo.Ellipsoid(new Vector3(-side * 0.09f, (i - 1) * 0.038f, 0.014f), new Vector3(0.004f, 0.008f, 0.004f), Cyan, 0.2f, 0.65f, 6, 3);
            }
            return geo.Build("passos_magicos golden paw with leaves and blue motes");
        }
    }
}
