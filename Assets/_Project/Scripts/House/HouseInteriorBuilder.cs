using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.House
{
    internal static class HouseInteriorBuilder
    {
        public static GameObject Build(Transform parent)
        {
            GameObject root = new("House Intermission Interior");
            root.transform.SetParent(parent, false);

            RuntimeHouseMaterials materials =
                root.AddComponent<RuntimeHouseMaterials>();

            BuildArchitecture(root.transform, materials);
            BuildWindow(root.transform, materials);
            BuildTatamiAndTeaArea(root.transform, materials);
            BuildComputerDesk(root.transform, materials);
            BuildWardrobe(root.transform, materials);
            BuildBedAndRug(root.transform, materials);
            BuildDecorations(root.transform, materials);
            BuildLighting(root.transform);

            return root;
        }

        private static void BuildArchitecture(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color wood = new(0.22f, 0.075f, 0.035f);
            Color darkWood = new(0.105f, 0.026f, 0.018f);
            Color plaster = new(0.83f, 0.62f, 0.38f);
            Color floor = new(0.30f, 0.12f, 0.055f);

            Part("Floor", PrimitiveType.Cube, parent,
                new Vector3(0f, -0.22f, 0f),
                new Vector3(18f, 0.44f, 12f), floor, materials);
            Part("Back Wall", PrimitiveType.Cube, parent,
                new Vector3(0f, 3.15f, 5.88f),
                new Vector3(18f, 6.3f, 0.26f), plaster, materials);
            Part("Right Wall", PrimitiveType.Cube, parent,
                new Vector3(8.88f, 3.15f, 0f),
                new Vector3(0.26f, 6.3f, 12f), plaster, materials);

            for (int index = -2; index <= 2; index++)
            {
                Part("Back Post", PrimitiveType.Cube, parent,
                    new Vector3(index * 4.4f, 3.2f, 5.66f),
                    new Vector3(0.24f, 6.55f, 0.28f), wood, materials);
            }

            for (int index = -1; index <= 1; index++)
            {
                Part("Right Post", PrimitiveType.Cube, parent,
                    new Vector3(8.65f, 3.2f, index * 4.6f),
                    new Vector3(0.28f, 6.55f, 0.24f), wood, materials);
            }

            Part("Back Lower Beam", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.28f, 5.62f),
                new Vector3(18.1f, 0.32f, 0.35f), darkWood, materials);
            Part("Back Upper Beam", PrimitiveType.Cube, parent,
                new Vector3(0f, 5.78f, 5.62f),
                new Vector3(18.1f, 0.42f, 0.38f), darkWood, materials);
            Part("Right Upper Beam", PrimitiveType.Cube, parent,
                new Vector3(8.62f, 5.78f, 0f),
                new Vector3(0.38f, 0.42f, 12f), darkWood, materials);
            Part("Front Step", PrimitiveType.Cube, parent,
                new Vector3(-0.5f, -0.05f, -6.15f),
                new Vector3(10f, 0.22f, 1.2f), darkWood, materials);
        }

        private static void BuildWindow(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color night = new(0.018f, 0.075f, 0.16f);
            Color frame = new(0.15f, 0.045f, 0.025f);
            Color moon = new(0.75f, 0.88f, 1f);
            Color garden = new(0.035f, 0.20f, 0.12f);

            Part("Night Window", PrimitiveType.Cube, parent,
                new Vector3(-5.65f, 3.05f, 5.70f),
                new Vector3(5.55f, 4.55f, 0.08f), night, materials, true);
            Part("Moon", PrimitiveType.Sphere, parent,
                new Vector3(-7.05f, 4.25f, 5.55f),
                new Vector3(0.95f, 0.95f, 0.16f), moon, materials, true);

            for (int index = -2; index <= 2; index++)
            {
                Part("Window Tree", PrimitiveType.Sphere, parent,
                    new Vector3(-5.6f + index * 0.72f,
                        1.3f + (index % 2 == 0 ? 0.45f : 0f), 5.48f),
                    new Vector3(1.15f, 1.35f, 0.18f), garden, materials);
            }

            Part("Window Top", PrimitiveType.Cube, parent,
                new Vector3(-5.65f, 5.35f, 5.48f),
                new Vector3(5.9f, 0.18f, 0.18f), frame, materials);
            Part("Window Bottom", PrimitiveType.Cube, parent,
                new Vector3(-5.65f, 0.75f, 5.48f),
                new Vector3(5.9f, 0.18f, 0.18f), frame, materials);
            for (int index = -2; index <= 2; index++)
            {
                Part("Window Frame", PrimitiveType.Cube, parent,
                    new Vector3(-5.65f + index * 1.4f, 3.05f, 5.47f),
                    new Vector3(0.11f, 4.7f, 0.18f), frame, materials);
            }
        }

        private static void BuildTatamiAndTeaArea(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color tatami = new(0.56f, 0.48f, 0.27f);
            Color border = new(0.15f, 0.12f, 0.07f);
            Color wood = new(0.25f, 0.08f, 0.035f);
            Color cushion = new(0.55f, 0.07f, 0.045f);

            for (int x = 0; x < 2; x++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 position = new(-5.3f + x * 2.75f, 0.055f,
                    -2.7f + z * 2.25f);
                Part("Tatami", PrimitiveType.Cube, parent, position,
                    new Vector3(2.55f, 0.11f, 2.05f), tatami, materials);
                Part("Tatami Border", PrimitiveType.Cube, parent,
                    position + new Vector3(0f, 0.065f, 0f),
                    new Vector3(2.68f, 0.025f, 2.18f), border, materials);
                Part("Tatami Surface", PrimitiveType.Cube, parent,
                    position + new Vector3(0f, 0.085f, 0f),
                    new Vector3(2.48f, 0.035f, 1.98f), tatami, materials);
            }

            Part("Tea Table Top", PrimitiveType.Cube, parent,
                new Vector3(-3.95f, 0.72f, -1.55f),
                new Vector3(3.35f, 0.22f, 2.2f), wood, materials);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Part("Tea Table Leg", PrimitiveType.Cube, parent,
                    new Vector3(-3.95f + x * 1.3f, 0.35f, -1.55f + z * 0.72f),
                    new Vector3(0.18f, 0.7f, 0.18f), wood, materials);
            }

            Vector3[] cushions =
            {
                new(-6.0f, 0.22f, -1.55f), new(-1.9f, 0.22f, -1.55f),
                new(-3.95f, 0.22f, -3.15f), new(-3.95f, 0.22f, 0.05f)
            };
            foreach (Vector3 position in cushions)
            {
                Part("Paw Cushion", PrimitiveType.Cube, parent, position,
                    new Vector3(1.15f, 0.22f, 1.05f), cushion, materials,
                    false, new Vector3(0f, 8f, 0f));
            }

            Part("Tea Pot", PrimitiveType.Sphere, parent,
                new Vector3(-4.2f, 1.05f, -1.5f),
                new Vector3(0.38f, 0.28f, 0.38f),
                new Color(0.20f, 0.30f, 0.22f), materials);
            Part("Tea Cup", PrimitiveType.Cylinder, parent,
                new Vector3(-3.45f, 0.98f, -1.35f),
                new Vector3(0.18f, 0.13f, 0.18f),
                new Color(0.86f, 0.67f, 0.38f), materials);
        }

        private static void BuildComputerDesk(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color wood = new(0.26f, 0.085f, 0.035f);
            Color dark = new(0.035f, 0.025f, 0.035f);
            Color screen = new(0.05f, 0.48f, 0.86f);

            Part("Computer Desk", PrimitiveType.Cube, parent,
                new Vector3(-2.6f, 1.35f, 4.55f),
                new Vector3(4.5f, 0.26f, 1.35f), wood, materials);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Desk Leg", PrimitiveType.Cube, parent,
                    new Vector3(-2.6f + side * 1.8f, 0.62f, 4.55f),
                    new Vector3(0.24f, 1.45f, 0.85f), wood, materials);
            }

            Part("Monitor", PrimitiveType.Cube, parent,
                new Vector3(-2.65f, 2.45f, 4.47f),
                new Vector3(2.35f, 1.35f, 0.18f), dark, materials,
                false, new Vector3(-5f, 0f, 0f));
            Part("Computer Shop Screen", PrimitiveType.Cube, parent,
                new Vector3(-2.65f, 2.45f, 4.35f),
                new Vector3(2.08f, 1.12f, 0.055f), screen, materials, true,
                new Vector3(-5f, 0f, 0f));
            Part("Monitor Stand", PrimitiveType.Cube, parent,
                new Vector3(-2.65f, 1.75f, 4.48f),
                new Vector3(0.24f, 0.55f, 0.24f), dark, materials);
            Part("Keyboard", PrimitiveType.Cube, parent,
                new Vector3(-2.65f, 1.55f, 3.98f),
                new Vector3(1.65f, 0.09f, 0.48f),
                new Color(0.72f, 0.50f, 0.28f), materials,
                false, new Vector3(8f, 0f, 0f));

            Part("Desk Chair Seat", PrimitiveType.Cube, parent,
                new Vector3(-2.65f, 0.72f, 2.95f),
                new Vector3(1.15f, 0.20f, 1.0f),
                new Color(0.52f, 0.09f, 0.055f), materials);
            Part("Desk Chair Back", PrimitiveType.Cube, parent,
                new Vector3(-2.65f, 1.35f, 3.38f),
                new Vector3(1.15f, 1.2f, 0.18f), wood, materials,
                false, new Vector3(-8f, 0f, 0f));
        }

        private static void BuildWardrobe(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color wood = new(0.20f, 0.055f, 0.025f);
            Color inside = new(0.085f, 0.025f, 0.018f);

            Part("Wardrobe Back", PrimitiveType.Cube, parent,
                new Vector3(2.05f, 3.05f, 5.05f),
                new Vector3(4.2f, 5.45f, 0.35f), inside, materials);
            Part("Wardrobe Left", PrimitiveType.Cube, parent,
                new Vector3(0.02f, 3.05f, 4.55f),
                new Vector3(0.25f, 5.6f, 1.25f), wood, materials);
            Part("Wardrobe Right", PrimitiveType.Cube, parent,
                new Vector3(4.08f, 3.05f, 4.55f),
                new Vector3(0.25f, 5.6f, 1.25f), wood, materials);
            Part("Wardrobe Top", PrimitiveType.Cube, parent,
                new Vector3(2.05f, 5.78f, 4.55f),
                new Vector3(4.3f, 0.28f, 1.35f), wood, materials);
            Part("Wardrobe Rail", PrimitiveType.Cylinder, parent,
                new Vector3(1.4f, 4.55f, 4.05f),
                new Vector3(0.08f, 1.15f, 0.08f),
                new Color(0.65f, 0.42f, 0.13f), materials,
                false, new Vector3(0f, 0f, 90f));

            Color[] clothes =
            {
                new(0.62f, 0.08f, 0.045f), new(0.08f, 0.12f, 0.18f),
                new(0.23f, 0.38f, 0.16f), new(0.47f, 0.28f, 0.12f)
            };
            for (int index = 0; index < clothes.Length; index++)
            {
                Part("Kin Outfit", PrimitiveType.Cube, parent,
                    new Vector3(0.55f + index * 0.58f, 3.55f, 4.02f),
                    new Vector3(0.45f, 1.65f, 0.16f),
                    clothes[index], materials);
            }

            for (int index = 0; index < 3; index++)
            {
                Part("Wardrobe Drawer", PrimitiveType.Cube, parent,
                    new Vector3(2.05f, 0.68f + index * 0.57f, 3.98f),
                    new Vector3(3.65f, 0.46f, 0.78f), wood, materials);
            }

            Part("Katana Rack", PrimitiveType.Cube, parent,
                new Vector3(2.05f, 6.35f, 5.30f),
                new Vector3(3.7f, 0.18f, 0.22f), wood, materials);
            Part("Katana", PrimitiveType.Cylinder, parent,
                new Vector3(2.05f, 6.62f, 5.20f),
                new Vector3(0.07f, 1.75f, 0.07f),
                new Color(0.78f, 0.42f, 0.08f), materials,
                true, new Vector3(0f, 0f, 90f));
        }

        private static void BuildBedAndRug(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color wood = new(0.24f, 0.07f, 0.03f);
            Color red = new(0.58f, 0.065f, 0.04f);
            Color cream = new(0.88f, 0.72f, 0.48f);

            Part("Paw Rug", PrimitiveType.Cube, parent,
                new Vector3(2.1f, 0.04f, -1.35f),
                new Vector3(6.8f, 0.08f, 5.5f), cream, materials,
                false, new Vector3(0f, -8f, 0f));
            Part("Rug Paw", PrimitiveType.Sphere, parent,
                new Vector3(2.1f, 0.10f, -1.5f),
                new Vector3(1.45f, 0.08f, 1.15f), red, materials);
            for (int index = 0; index < 4; index++)
            {
                float x = 0.9f + index * 0.8f;
                float z = -0.35f + Mathf.Abs(index - 1.5f) * 0.22f;
                Part("Rug Toe", PrimitiveType.Sphere, parent,
                    new Vector3(x, 0.11f, z),
                    new Vector3(0.50f, 0.07f, 0.63f), red, materials);
            }

            Part("Bed Frame", PrimitiveType.Cube, parent,
                new Vector3(6.1f, 0.55f, 1.55f),
                new Vector3(4.2f, 0.48f, 5.0f), wood, materials);
            Part("Mattress", PrimitiveType.Cube, parent,
                new Vector3(6.1f, 0.94f, 1.55f),
                new Vector3(3.72f, 0.38f, 4.45f), cream, materials);
            Part("Blanket", PrimitiveType.Cube, parent,
                new Vector3(6.1f, 1.18f, 0.9f),
                new Vector3(3.76f, 0.12f, 3.0f), red, materials);
            Part("Pillow", PrimitiveType.Sphere, parent,
                new Vector3(6.1f, 1.35f, 3.1f),
                new Vector3(1.55f, 0.32f, 0.72f), cream, materials);

            Part("Cat Bed", PrimitiveType.Cylinder, parent,
                new Vector3(1.25f, 0.28f, 0.35f),
                new Vector3(1.18f, 0.18f, 1.18f), red, materials);
            Part("Cat Bed Center", PrimitiveType.Cylinder, parent,
                new Vector3(1.25f, 0.47f, 0.35f),
                new Vector3(0.82f, 0.10f, 0.82f), cream, materials);
        }

        private static void BuildDecorations(
            Transform parent,
            RuntimeHouseMaterials materials)
        {
            Color wood = new(0.22f, 0.06f, 0.025f);
            Color gold = new(0.90f, 0.48f, 0.10f);
            Color green = new(0.10f, 0.34f, 0.14f);

            Part("Cat Crest", PrimitiveType.Cylinder, parent,
                new Vector3(-0.2f, 4.6f, 5.45f),
                new Vector3(0.95f, 0.10f, 0.95f), gold, materials,
                true, new Vector3(90f, 0f, 0f));

            for (int index = 0; index < 3; index++)
            {
                Part("Shelf", PrimitiveType.Cube, parent,
                    new Vector3(7.55f, 1.15f + index * 1.25f, 5.25f),
                    new Vector3(1.65f, 0.18f, 0.7f), wood, materials);
                Part("Plant Pot", PrimitiveType.Cylinder, parent,
                    new Vector3(7.55f, 1.47f + index * 1.25f, 5.18f),
                    new Vector3(0.30f, 0.30f, 0.30f),
                    new Color(0.48f, 0.18f, 0.06f), materials);
                Part("Plant", PrimitiveType.Sphere, parent,
                    new Vector3(7.55f, 1.95f + index * 1.25f, 5.18f),
                    new Vector3(0.65f, 0.55f, 0.45f), green, materials);
            }

            Vector3[] lanterns =
            {
                new(-8.0f, 0.65f, -4.9f), new(-0.6f, 0.65f, 4.7f),
                new(7.8f, 0.65f, -4.5f)
            };
            foreach (Vector3 position in lanterns)
            {
                BuildLantern(parent, position, materials);
            }
        }

        private static void BuildLantern(
            Transform parent,
            Vector3 position,
            RuntimeHouseMaterials materials)
        {
            Color dark = new(0.12f, 0.035f, 0.02f);
            Color light = new(1f, 0.58f, 0.16f);
            Part("Lantern Base", PrimitiveType.Cylinder, parent, position,
                new Vector3(0.42f, 0.10f, 0.42f), dark, materials);
            Part("Lantern Glow", PrimitiveType.Cube, parent,
                position + new Vector3(0f, 0.45f, 0f),
                new Vector3(0.55f, 0.72f, 0.55f), light, materials, true);
            Part("Lantern Top", PrimitiveType.Cylinder, parent,
                position + new Vector3(0f, 0.88f, 0f),
                new Vector3(0.48f, 0.10f, 0.48f), dark, materials);
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject moonLight = new("House Moon Light");
            moonLight.transform.SetParent(parent, false);
            moonLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light directional = moonLight.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(0.40f, 0.52f, 0.78f);
            directional.intensity = 0.42f;
            directional.shadows = LightShadows.None;

            GameObject warmLight = new("House Warm Light");
            warmLight.transform.SetParent(parent, false);
            warmLight.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            Light point = warmLight.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1f, 0.48f, 0.16f);
            point.intensity = 8f;
            point.range = 18f;
            point.shadows = LightShadows.None;
        }

        private static GameObject Part(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color,
            RuntimeHouseMaterials materials,
            bool emissive = false,
            Vector3 rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation);
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = materials.Get(name, color, emissive);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }
    }

    internal sealed class RuntimeHouseMaterials : MonoBehaviour
    {
        private readonly Dictionary<string, Material> materials = new();

        public Material Get(string label, Color color, bool emissive)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + emissive;
            if (materials.TryGetValue(key, out Material material))
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            shader ??= Shader.Find("Sprites/Default");

            material = new Material(shader)
            {
                name = $"House {label}",
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }

            materials[key] = material;
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials.Values)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
            materials.Clear();
        }
    }
}
