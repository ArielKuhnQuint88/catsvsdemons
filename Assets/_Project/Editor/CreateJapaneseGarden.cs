using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsVsDemons.Editor
{
    public static class CreateJapaneseGarden
    {
        private const string GameScene =
            "Assets/_Project/Scenes/Game.unity";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Garden";

        [MenuItem("Tools/Cats vs Demons/Create Japanese Garden 3D")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(
                GameScene,
                OpenSceneMode.Single
            );

            GameObject oldGarden = GameObject.Find("JapaneseGarden3D");
            if (oldGarden != null)
            {
                Object.DestroyImmediate(oldGarden);
            }

            EnsureFolders();

            GameObject root = new GameObject("JapaneseGarden3D");
            Transform environment = GameObject.Find("Game/Environment")?.transform;
            if (environment != null)
            {
                root.transform.SetParent(environment);
            }

            BuildGround(root.transform);
            BuildHouse(root.transform);
            BuildPond(root.transform, new Vector3(-15f, 0f, -9.5f), 0f);
            BuildPond(root.transform, new Vector3(15f, 0f, 9.5f), 180f);
            BuildMeditationArea(root.transform, new Vector3(-14f, 0f, 10f), 20f);
            BuildMeditationArea(root.transform, new Vector3(14f, 0f, -10f), -20f);
            BuildVegetation(root.transform);
            BuildGardenDetails(root.transform);
            ConfigureLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScene);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Jardim japonês 3D criado. Aperte Play para explorar.",
                "OK"
            );
        }

        private static void BuildGround(Transform parent)
        {
            CreatePart(
                "GardenGrass",
                PrimitiveType.Cube,
                parent,
                new Vector3(0f, -0.18f, 0f),
                new Vector3(43f, 0.3f, 33f),
                new Color(0.16f, 0.43f, 0.2f)
            );

            CreatePart(
                "InnerGarden",
                PrimitiveType.Cube,
                parent,
                new Vector3(0f, -0.01f, 0f),
                new Vector3(16f, 0.12f, 12f),
                new Color(0.27f, 0.5f, 0.24f)
            );
        }

        private static void BuildHouse(Transform parent)
        {
            GameObject oldHouse = GameObject.Find("Game/Environment/House");
            if (oldHouse != null)
            {
                Renderer renderer = oldHouse.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            GameObject house = new GameObject("JapaneseHouse");
            house.transform.SetParent(parent);
            house.transform.position = Vector3.zero;

            Color wood = new Color(0.24f, 0.09f, 0.045f);
            Color plaster = new Color(0.91f, 0.84f, 0.68f);
            Color roof = new Color(0.08f, 0.1f, 0.12f);
            Color red = new Color(0.56f, 0.055f, 0.04f);
            Color paper = new Color(1f, 0.88f, 0.62f);

            CreatePart("Foundation", PrimitiveType.Cube, house.transform,
                new Vector3(0f, 0.25f, 0f), new Vector3(7.6f, 0.5f, 5.8f), wood);
            CreatePart("MainWalls", PrimitiveType.Cube, house.transform,
                new Vector3(0f, 1.9f, 0f), new Vector3(6.8f, 3.1f, 5f), plaster);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int z = -2; z <= 2; z += 2)
                {
                    CreatePart("WoodPost", PrimitiveType.Cube, house.transform,
                        new Vector3(side * 3.55f, 1.9f, z),
                        new Vector3(0.22f, 3.5f, 0.22f), wood);
                }
            }

            for (int x = -2; x <= 2; x += 2)
            {
                CreatePart("ShojiDoor", PrimitiveType.Cube, house.transform,
                    new Vector3(x, 1.75f, -2.53f),
                    new Vector3(1.75f, 2.75f, 0.1f), paper);
                CreatePart("ShojiFrameV", PrimitiveType.Cube, house.transform,
                    new Vector3(x, 1.75f, -2.61f),
                    new Vector3(0.08f, 2.75f, 0.08f), wood);
                for (int row = 0; row < 3; row++)
                {
                    CreatePart("ShojiFrameH", PrimitiveType.Cube, house.transform,
                        new Vector3(x, 0.85f + row * 0.85f, -2.62f),
                        new Vector3(1.75f, 0.06f, 0.08f), wood);
                }
            }

            CreateRoof(house.transform, new Vector3(0f, 3.75f, 0f),
                new Vector3(8.5f, 0.45f, 6.8f), roof);
            CreatePart("UpperFloor", PrimitiveType.Cube, house.transform,
                new Vector3(0f, 4.45f, 0f), new Vector3(4.2f, 1.3f, 3.2f), red);
            CreateRoof(house.transform, new Vector3(0f, 5.35f, 0f),
                new Vector3(5.5f, 0.38f, 4.4f), roof);

            CreatePart("Ridge", PrimitiveType.Cylinder, house.transform,
                new Vector3(0f, 5.75f, 0f), new Vector3(0.16f, 2.3f, 0.16f),
                red, new Vector3(0f, 0f, 90f));

            for (int side = -1; side <= 1; side += 2)
            {
                CreateLantern(house.transform, new Vector3(side * 3.1f, 1.45f, -2.9f));
            }
        }

        private static void CreateRoof(
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            CreatePart("RoofLeft", PrimitiveType.Cube, parent,
                position + new Vector3(-scale.x * 0.22f, 0f, 0f),
                new Vector3(scale.x * 0.56f, scale.y, scale.z),
                color, new Vector3(0f, 0f, -8f));
            CreatePart("RoofRight", PrimitiveType.Cube, parent,
                position + new Vector3(scale.x * 0.22f, 0f, 0f),
                new Vector3(scale.x * 0.56f, scale.y, scale.z),
                color, new Vector3(0f, 0f, 8f));
        }

        private static void BuildPond(Transform parent, Vector3 position, float yaw)
        {
            GameObject pond = new GameObject("KoiPond");
            pond.transform.SetParent(parent);
            pond.transform.position = position;
            pond.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            CreatePart("Water", PrimitiveType.Cylinder, pond.transform,
                new Vector3(0f, 0.02f, 0f), new Vector3(4.2f, 0.08f, 2.7f),
                new Color(0.05f, 0.42f, 0.58f));

            for (int index = 0; index < 14; index++)
            {
                float angle = index * Mathf.PI * 2f / 14f;
                Vector3 rockPosition = new Vector3(
                    Mathf.Cos(angle) * 4.1f,
                    0.18f,
                    Mathf.Sin(angle) * 2.65f
                );
                CreatePart("PondRock", PrimitiveType.Sphere, pond.transform,
                    rockPosition,
                    new Vector3(0.75f, 0.42f, 0.65f),
                    new Color(0.29f, 0.31f, 0.29f));
            }

            Color[] koiColors =
            {
                new Color(1f, 0.3f, 0.04f),
                new Color(1f, 0.86f, 0.58f),
                new Color(0.12f, 0.12f, 0.1f)
            };
            Vector3[] koiPositions =
            {
                new Vector3(-1.5f, 0.18f, -0.3f),
                new Vector3(0.4f, 0.18f, 0.65f),
                new Vector3(1.6f, 0.18f, -0.5f),
                new Vector3(-0.25f, 0.18f, -0.8f)
            };

            for (int index = 0; index < koiPositions.Length; index++)
            {
                CreatePart("Koi", PrimitiveType.Capsule, pond.transform,
                    koiPositions[index], new Vector3(0.18f, 0.48f, 0.13f),
                    koiColors[index % koiColors.Length],
                    new Vector3(90f, index * 37f, 0f));
            }

            CreatePart("Bridge", PrimitiveType.Cube, pond.transform,
                new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.18f, 5.2f),
                new Color(0.58f, 0.06f, 0.035f),
                new Vector3(0f, 0f, 5f));
        }

        private static void BuildMeditationArea(
            Transform parent,
            Vector3 position,
            float yaw)
        {
            GameObject area = new GameObject("MeditationArea");
            area.transform.SetParent(parent);
            area.transform.position = position;
            area.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            CreatePart("RakedSand", PrimitiveType.Cylinder, area.transform,
                new Vector3(0f, 0.02f, 0f), new Vector3(3.1f, 0.08f, 3.1f),
                new Color(0.82f, 0.75f, 0.58f));
            CreatePart("MeditationMat", PrimitiveType.Cube, area.transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(1.4f, 0.12f, 1.8f),
                new Color(0.48f, 0.18f, 0.11f));

            Vector3[] stones =
            {
                new Vector3(-1.7f, 0.25f, 0.5f),
                new Vector3(1.65f, 0.18f, 0.9f),
                new Vector3(1.25f, 0.32f, -1.25f)
            };
            foreach (Vector3 stone in stones)
            {
                CreatePart("ZenStone", PrimitiveType.Sphere, area.transform,
                    stone, new Vector3(0.72f, 0.45f, 0.6f),
                    new Color(0.27f, 0.28f, 0.26f));
            }
        }

        private static void BuildVegetation(Transform parent)
        {
            Vector3[] cherryPositions =
            {
                new Vector3(-19f, 0f, 12f),
                new Vector3(19f, 0f, -12f),
                new Vector3(-18f, 0f, -13f),
                new Vector3(18f, 0f, 13f),
                new Vector3(-8f, 0f, 14f),
                new Vector3(9f, 0f, -14f)
            };
            foreach (Vector3 position in cherryPositions)
            {
                CreateCherryTree(parent, position);
            }

            for (int index = 0; index < 7; index++)
            {
                CreateBambooCluster(
                    parent,
                    new Vector3(-20f + index * 1.05f, 0f, 15f)
                );
                CreateBambooCluster(
                    parent,
                    new Vector3(20f - index * 1.05f, 0f, -15f)
                );
            }
        }

        private static void CreateCherryTree(Transform parent, Vector3 position)
        {
            GameObject tree = new GameObject("CherryTree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;

            CreatePart("Trunk", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0f, 1.7f, 0f), new Vector3(0.42f, 1.7f, 0.42f),
                new Color(0.25f, 0.09f, 0.06f));
            Color blossom = new Color(1f, 0.48f, 0.68f);
            CreatePart("Blossoms", PrimitiveType.Sphere, tree.transform,
                new Vector3(0f, 3.8f, 0f), new Vector3(2.4f, 1.55f, 2.2f), blossom);
            CreatePart("Blossoms", PrimitiveType.Sphere, tree.transform,
                new Vector3(-1.15f, 3.3f, 0.25f), new Vector3(1.5f, 1.15f, 1.4f), blossom);
            CreatePart("Blossoms", PrimitiveType.Sphere, tree.transform,
                new Vector3(1.05f, 3.45f, -0.2f), new Vector3(1.6f, 1.2f, 1.5f), blossom);
        }

        private static void CreateBambooCluster(Transform parent, Vector3 position)
        {
            GameObject bamboo = new GameObject("Bamboo");
            bamboo.transform.SetParent(parent);
            bamboo.transform.position = position;
            Color green = new Color(0.18f, 0.58f, 0.19f);

            for (int index = 0; index < 3; index++)
            {
                float height = 2.8f + index * 0.55f;
                CreatePart("BambooStem", PrimitiveType.Cylinder, bamboo.transform,
                    new Vector3((index - 1) * 0.28f, height * 0.5f, index * 0.12f),
                    new Vector3(0.11f, height * 0.5f, 0.11f), green);
            }
        }

        private static void BuildGardenDetails(Transform parent)
        {
            Vector3[] lanterns =
            {
                new Vector3(-7f, 0f, -5f),
                new Vector3(7f, 0f, -5f),
                new Vector3(-7f, 0f, 5f),
                new Vector3(7f, 0f, 5f)
            };
            foreach (Vector3 position in lanterns)
            {
                CreateLantern(parent, position);
            }

            for (int index = -4; index <= 4; index++)
            {
                CreatePart("SteppingStone", PrimitiveType.Cylinder, parent,
                    new Vector3(index * 1.45f, 0.05f, 7.2f),
                    new Vector3(0.55f, 0.08f, 0.42f),
                    new Color(0.4f, 0.42f, 0.39f));
            }
        }

        private static void CreateLantern(Transform parent, Vector3 position)
        {
            GameObject lantern = new GameObject("GardenLantern");
            lantern.transform.SetParent(parent);
            lantern.transform.localPosition = position;

            Color stone = new Color(0.42f, 0.45f, 0.43f);
            CreatePart("LanternBase", PrimitiveType.Cylinder, lantern.transform,
                new Vector3(0f, 0.18f, 0f), new Vector3(0.4f, 0.18f, 0.4f), stone);
            CreatePart("LanternPost", PrimitiveType.Cube, lantern.transform,
                new Vector3(0f, 0.85f, 0f), new Vector3(0.28f, 1.2f, 0.28f), stone);
            CreatePart("LanternLight", PrimitiveType.Cube, lantern.transform,
                new Vector3(0f, 1.55f, 0f), new Vector3(0.7f, 0.55f, 0.7f),
                new Color(1f, 0.58f, 0.1f));
            CreatePart("LanternTop", PrimitiveType.Cylinder, lantern.transform,
                new Vector3(0f, 1.92f, 0f), new Vector3(0.58f, 0.16f, 0.58f), stone);
        }

        private static GameObject CreatePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color,
            Vector3? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = GetMaterial(color);
            return part;
        }

        private static Material GetMaterial(Color color)
        {
            Color32 value = color;
            string name = $"Garden_{value.r:X2}{value.g:X2}{value.b:X2}";
            string path = $"{MaterialFolder}/{name}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientMode =
                UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.36f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.16f, 0.12f);

            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.color = new Color(1f, 0.84f, 0.68f);
                sun.intensity = 1.25f;
                sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            }
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/_Project/Art/Materials",
                MaterialFolder
            };

            foreach (string folder in folders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                string parent = Path.GetDirectoryName(folder)
                    ?.Replace('\\', '/');
                string name = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
