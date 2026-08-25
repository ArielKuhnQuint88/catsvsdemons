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
            BuildFlowerGardens(root.transform);
            BuildGardenDetails(root.transform);
            BuildWorldExtension(root.transform);
            ConfigureLighting();
            ConfigureSky();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScene);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Cats vs Demons",
                "Jardim, horizonte e céu imersivo criados. Aperte Play para explorar.",
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

            Color shore = new Color(0.46f, 0.38f, 0.27f);
            Color deepWater = new Color(0.025f, 0.23f, 0.32f);
            Color clearWater = new Color(0.06f, 0.52f, 0.62f);

            CreatePart("PondBed", PrimitiveType.Cylinder, pond.transform,
                new Vector3(0f, -0.05f, 0f), new Vector3(4.75f, 0.12f, 3.15f),
                shore);
            CreatePart("DeepWater", PrimitiveType.Cylinder, pond.transform,
                new Vector3(0f, 0.015f, 0f), new Vector3(4.35f, 0.08f, 2.75f),
                deepWater);
            CreatePart("WaterHighlight", PrimitiveType.Cylinder, pond.transform,
                new Vector3(-0.18f, 0.095f, 0.12f), new Vector3(4.02f, 0.018f, 2.48f),
                clearWater);

            Color[] rockColors =
            {
                new Color(0.25f, 0.29f, 0.27f),
                new Color(0.36f, 0.37f, 0.32f),
                new Color(0.23f, 0.33f, 0.28f)
            };
            for (int index = 0; index < 22; index++)
            {
                float angle = index * Mathf.PI * 2f / 22f;
                float wobble = 1f + Mathf.Sin(index * 2.17f) * 0.06f;
                Vector3 rockPosition = new Vector3(
                    Mathf.Cos(angle) * 4.48f * wobble,
                    0.18f,
                    Mathf.Sin(angle) * 2.9f * wobble
                );
                float size = 0.56f + (index % 4) * 0.08f;
                CreatePart("PondRock", PrimitiveType.Sphere, pond.transform,
                    rockPosition,
                    new Vector3(size, 0.30f + (index % 3) * 0.06f, size * 0.82f),
                    rockColors[index % rockColors.Length],
                    new Vector3(0f, index * 29f, 0f));
            }

            Color[] koiColors =
            {
                new Color(1f, 0.22f, 0.025f),
                new Color(1f, 0.82f, 0.42f),
                new Color(0.96f, 0.96f, 0.90f),
                new Color(0.12f, 0.12f, 0.1f)
            };
            Vector3[] koiPositions =
            {
                new Vector3(-1.55f, 0.16f, -0.32f),
                new Vector3(0.45f, 0.16f, 0.72f),
                new Vector3(1.55f, 0.16f, -0.52f),
                new Vector3(-0.2f, 0.16f, -0.9f),
                new Vector3(1.0f, 0.16f, 0.08f)
            };

            for (int index = 0; index < koiPositions.Length; index++)
            {
                GameObject koi = new GameObject("Koi");
                koi.transform.SetParent(pond.transform);
                koi.transform.localPosition = koiPositions[index];
                koi.transform.localRotation = Quaternion.Euler(0f, index * 47f, 0f);
                CreatePart("Body", PrimitiveType.Capsule, koi.transform,
                    Vector3.zero, new Vector3(0.16f, 0.42f, 0.12f),
                    koiColors[index % koiColors.Length], new Vector3(90f, 0f, 0f));
                CreatePart("Tail", PrimitiveType.Sphere, koi.transform,
                    new Vector3(0f, 0f, -0.43f), new Vector3(0.20f, 0.04f, 0.22f),
                    koiColors[(index + 1) % koiColors.Length],
                    new Vector3(0f, 0f, 45f));
            }

            Color lily = new Color(0.18f, 0.48f, 0.20f);
            Vector3[] lilyPositions =
            {
                new Vector3(-2.5f, 0.15f, 0.8f),
                new Vector3(2.35f, 0.15f, 0.72f),
                new Vector3(-2.1f, 0.15f, -1.15f),
                new Vector3(2.65f, 0.15f, -0.75f)
            };
            foreach (Vector3 lilyPosition in lilyPositions)
            {
                CreatePart("LilyPad", PrimitiveType.Cylinder, pond.transform,
                    lilyPosition, new Vector3(0.42f, 0.025f, 0.34f), lily);
            }
            CreateFlower(pond.transform, new Vector3(-2.5f, 0.23f, 0.8f),
                new Color(1f, 0.56f, 0.76f), 0.42f);

            Color bridgeRed = new Color(0.58f, 0.055f, 0.03f);
            Color bridgeDark = new Color(0.18f, 0.055f, 0.035f);
            for (int plank = -4; plank <= 4; plank++)
            {
                float arch = 0.48f + (1f - Mathf.Abs(plank) / 5f) * 0.35f;
                CreatePart("BridgePlank", PrimitiveType.Cube, pond.transform,
                    new Vector3(0f, arch, plank * 0.57f),
                    new Vector3(1.35f, 0.14f, 0.5f), bridgeRed,
                    new Vector3(plank * -1.4f, 0f, 0f));
            }
            foreach (float side in new[] { -0.82f, 0.82f })
            {
                CreatePart("BridgeRail", PrimitiveType.Cube, pond.transform,
                    new Vector3(side, 1.25f, 0f), new Vector3(0.09f, 0.09f, 5.2f),
                    bridgeDark, new Vector3(0f, 0f, side * -5f));
                for (int post = -2; post <= 2; post++)
                {
                    CreatePart("BridgePost", PrimitiveType.Cylinder, pond.transform,
                        new Vector3(side, 0.92f, post * 1.15f),
                        new Vector3(0.10f, 0.55f, 0.10f), bridgeDark);
                }
            }
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
                new Vector3(-8f, 0f, 14f)
            };
            foreach (Vector3 position in cherryPositions)
            {
                CreateCherryTree(parent, position);
            }

            Vector3[] maplePositions =
            {
                new Vector3(-13f, 0f, 14f),
                new Vector3(14f, 0f, 13f),
                new Vector3(-19f, 0f, -4f),
                new Vector3(19f, 0f, 3f)
            };
            for (int index = 0; index < maplePositions.Length; index++)
            {
                CreateMapleTree(parent, maplePositions[index], index % 2 == 0);
            }

            Vector3[] pinePositions =
            {
                new Vector3(-10f, 0f, -14f),
                new Vector3(10f, 0f, 14f),
                new Vector3(-20f, 0f, 5f),
                new Vector3(20f, 0f, -5f)
            };
            foreach (Vector3 position in pinePositions)
            {
                CreatePineTree(parent, position);
            }

            for (int index = 0; index < 7; index++)
            {
                CreateBambooCluster(parent, new Vector3(-20f + index * 1.05f, 0f, 15f));
                CreateBambooCluster(parent, new Vector3(20f - index * 1.05f, 0f, -15f));
            }
        }

        private static void CreateCherryTree(Transform parent, Vector3 position)
        {
            GameObject tree = new GameObject("CherryTree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;

            Color trunk = new Color(0.22f, 0.075f, 0.045f);
            Color pink = new Color(1f, 0.45f, 0.68f);
            Color palePink = new Color(1f, 0.69f, 0.80f);
            CreatePart("Trunk", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0f, 1.7f, 0f), new Vector3(0.38f, 1.7f, 0.38f), trunk);
            CreatePart("Branch", PrimitiveType.Cylinder, tree.transform,
                new Vector3(-0.55f, 2.75f, 0f), new Vector3(0.16f, 1.0f, 0.16f),
                trunk, new Vector3(0f, 0f, -35f));
            CreatePart("Branch", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0.65f, 2.8f, 0.1f), new Vector3(0.14f, 0.95f, 0.14f),
                trunk, new Vector3(0f, 0f, 38f));

            Vector3[] crowns =
            {
                new Vector3(0f, 3.85f, 0f),
                new Vector3(-1.05f, 3.42f, 0.28f),
                new Vector3(1.0f, 3.52f, -0.18f),
                new Vector3(-0.45f, 4.35f, -0.35f)
            };
            for (int index = 0; index < crowns.Length; index++)
            {
                CreatePart("Blossoms", PrimitiveType.Sphere, tree.transform,
                    crowns[index], new Vector3(1.45f, 0.9f, 1.3f),
                    index % 2 == 0 ? pink : palePink);
            }
        }

        private static void CreateMapleTree(Transform parent, Vector3 position, bool red)
        {
            GameObject tree = new GameObject("JapaneseMaple");
            tree.transform.SetParent(parent);
            tree.transform.position = position;
            Color trunk = new Color(0.25f, 0.10f, 0.05f);
            Color leafA = red
                ? new Color(0.72f, 0.08f, 0.035f)
                : new Color(0.93f, 0.32f, 0.045f);
            Color leafB = red
                ? new Color(0.95f, 0.19f, 0.06f)
                : new Color(0.96f, 0.56f, 0.06f);

            CreatePart("Trunk", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0f, 1.45f, 0f), new Vector3(0.34f, 1.45f, 0.34f), trunk);
            for (int index = 0; index < 5; index++)
            {
                float angle = index * Mathf.PI * 2f / 5f;
                CreatePart("MapleCrown", PrimitiveType.Sphere, tree.transform,
                    new Vector3(Mathf.Cos(angle) * 1.05f, 3.1f + (index % 2) * 0.35f,
                        Mathf.Sin(angle) * 0.9f),
                    new Vector3(1.25f, 0.72f, 1.05f),
                    index % 2 == 0 ? leafA : leafB);
            }
        }

        private static void CreatePineTree(Transform parent, Vector3 position)
        {
            GameObject tree = new GameObject("JapanesePine");
            tree.transform.SetParent(parent);
            tree.transform.position = position;
            Color trunk = new Color(0.20f, 0.09f, 0.045f);
            Color pine = new Color(0.055f, 0.30f, 0.14f);
            Color pineLight = new Color(0.08f, 0.40f, 0.18f);

            CreatePart("Trunk", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0f, 1.75f, 0f), new Vector3(0.38f, 1.75f, 0.38f), trunk);
            Vector3[] crowns =
            {
                new Vector3(-0.75f, 2.65f, 0f),
                new Vector3(0.65f, 3.15f, 0.15f),
                new Vector3(-0.15f, 3.75f, -0.1f)
            };
            for (int index = 0; index < crowns.Length; index++)
            {
                CreatePart("PineCrown", PrimitiveType.Sphere, tree.transform,
                    crowns[index], new Vector3(1.55f, 0.48f, 1.2f),
                    index % 2 == 0 ? pine : pineLight);
            }
        }

        private static void CreateBambooCluster(Transform parent, Vector3 position)
        {
            GameObject bamboo = new GameObject("Bamboo");
            bamboo.transform.SetParent(parent);
            bamboo.transform.position = position;
            Color green = new Color(0.16f, 0.54f, 0.16f);
            Color leaf = new Color(0.08f, 0.36f, 0.12f);

            for (int index = 0; index < 3; index++)
            {
                float height = 2.8f + index * 0.55f;
                float x = (index - 1) * 0.28f;
                CreatePart("BambooStem", PrimitiveType.Cylinder, bamboo.transform,
                    new Vector3(x, height * 0.5f, index * 0.12f),
                    new Vector3(0.095f, height * 0.5f, 0.095f), green);
                CreatePart("BambooLeaves", PrimitiveType.Sphere, bamboo.transform,
                    new Vector3(x + 0.15f, height - 0.25f, index * 0.12f),
                    new Vector3(0.48f, 0.16f, 0.22f), leaf,
                    new Vector3(0f, 0f, index % 2 == 0 ? 22f : -22f));
            }
        }

        private static void BuildFlowerGardens(Transform parent)
        {
            Vector3[] patches =
            {
                new Vector3(-10.5f, 0f, 6.8f),
                new Vector3(10.5f, 0f, -6.8f),
                new Vector3(-5.5f, 0f, 10.2f),
                new Vector3(6.2f, 0f, -10.4f),
                new Vector3(-16.5f, 0f, 4.5f),
                new Vector3(16.2f, 0f, -3.8f)
            };
            Color[] colors =
            {
                new Color(1f, 0.38f, 0.62f),
                new Color(0.78f, 0.38f, 0.95f),
                new Color(1f, 0.78f, 0.14f),
                new Color(0.95f, 0.95f, 0.88f)
            };
            for (int index = 0; index < patches.Length; index++)
            {
                CreateFlowerPatch(parent, patches[index], colors[index % colors.Length]);
            }
        }

        private static void CreateFlowerPatch(Transform parent, Vector3 position, Color color)
        {
            GameObject patch = new GameObject("FlowerPatch");
            patch.transform.SetParent(parent);
            patch.transform.localPosition = position;
            for (int index = 0; index < 7; index++)
            {
                float angle = index * 2.399f;
                float radius = 0.25f + (index % 3) * 0.28f;
                CreateFlower(patch.transform,
                    new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius),
                    index % 3 == 0 ? new Color(1f, 0.9f, 0.28f) : color,
                    0.32f + (index % 2) * 0.08f);
            }
        }

        private static void CreateFlower(
            Transform parent,
            Vector3 position,
            Color petalColor,
            float size)
        {
            GameObject flower = new GameObject("Flower");
            flower.transform.SetParent(parent);
            flower.transform.localPosition = position;
            Color stem = new Color(0.10f, 0.42f, 0.12f);
            CreatePart("Stem", PrimitiveType.Cylinder, flower.transform,
                new Vector3(0f, size * 0.6f, 0f),
                new Vector3(size * 0.08f, size * 0.6f, size * 0.08f), stem);
            for (int petal = 0; petal < 5; petal++)
            {
                float angle = petal * Mathf.PI * 2f / 5f;
                CreatePart("Petal", PrimitiveType.Sphere, flower.transform,
                    new Vector3(Mathf.Cos(angle) * size * 0.32f, size * 1.25f,
                        Mathf.Sin(angle) * size * 0.32f),
                    new Vector3(size * 0.28f, size * 0.10f, size * 0.2f), petalColor,
                    new Vector3(0f, -petal * 72f, 0f));
            }
            CreatePart("FlowerCenter", PrimitiveType.Sphere, flower.transform,
                new Vector3(0f, size * 1.27f, 0f),
                Vector3.one * size * 0.22f, new Color(1f, 0.72f, 0.05f));
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

        private static void BuildWorldExtension(Transform parent)
        {
            GameObject world = new GameObject("WorldExtension");
            world.transform.SetParent(parent);

            CreatePart("OuterWater", PrimitiveType.Cube, world.transform,
                new Vector3(0f, -0.78f, 0f), new Vector3(112f, 0.28f, 88f),
                new Color(0.035f, 0.2f, 0.28f));
            CreatePart("OuterIsland", PrimitiveType.Cube, world.transform,
                new Vector3(0f, -0.48f, 0f), new Vector3(76f, 0.38f, 57f),
                new Color(0.10f, 0.30f, 0.16f));

            Vector3[] mountainPositions =
            {
                new Vector3(-39f, 3.8f, 18f),
                new Vector3(-31f, 5.2f, 27f),
                new Vector3(-15f, 4.1f, 31f),
                new Vector3(5f, 6.2f, 34f),
                new Vector3(24f, 4.8f, 30f),
                new Vector3(39f, 4.3f, 18f),
                new Vector3(42f, 3.4f, -7f),
                new Vector3(-42f, 3.6f, -10f)
            };
            for (int index = 0; index < mountainPositions.Length; index++)
            {
                float height = 7f + (index % 3) * 2.3f;
                CreateMountain(
                    world.transform,
                    mountainPositions[index],
                    new Vector3(10f + (index % 2) * 3f, height, 8f)
                );
            }

            Vector3[] distantTrees =
            {
                new Vector3(-29f, 0f, 19f),
                new Vector3(-23f, 0f, 23f),
                new Vector3(27f, 0f, 21f),
                new Vector3(32f, 0f, 14f),
                new Vector3(-31f, 0f, -18f),
                new Vector3(30f, 0f, -18f),
                new Vector3(-12f, 0f, -24f),
                new Vector3(13f, 0f, -25f)
            };
            foreach (Vector3 position in distantTrees)
            {
                CreateCherryTree(world.transform, position);
            }

            CreateTorii(world.transform, new Vector3(0f, 0f, 24f), 180f);
            CreateTorii(world.transform, new Vector3(-31f, 0f, -3f), 90f);

            for (int index = -5; index <= 5; index++)
            {
                CreatePart("ShoreRock", PrimitiveType.Sphere, world.transform,
                    new Vector3(index * 6.5f, -0.08f, -27f + Mathf.Abs(index) * 0.45f),
                    new Vector3(2.2f, 0.9f, 1.55f),
                    new Color(0.20f, 0.25f, 0.23f),
                    new Vector3(0f, index * 13f, 0f));
            }
        }

        private static void CreateMountain(
            Transform parent,
            Vector3 position,
            Vector3 scale)
        {
            GameObject mountain = new GameObject("DistantMountain");
            mountain.transform.SetParent(parent);
            mountain.transform.localPosition = position;

            CreatePart("MountainBody", PrimitiveType.Sphere, mountain.transform,
                Vector3.zero, scale,
                new Color(0.13f, 0.23f, 0.19f),
                new Vector3(0f, position.x * 1.7f, -8f));
            CreatePart("MountainForest", PrimitiveType.Sphere, mountain.transform,
                new Vector3(0f, scale.y * 0.12f, -0.2f),
                new Vector3(scale.x * 0.82f, scale.y * 0.82f, scale.z * 0.84f),
                new Color(0.09f, 0.32f, 0.17f));
        }

        private static void CreateTorii(Transform parent, Vector3 position, float yaw)
        {
            GameObject torii = new GameObject("DistantTorii");
            torii.transform.SetParent(parent);
            torii.transform.localPosition = position;
            torii.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Color red = new Color(0.58f, 0.045f, 0.025f);
            Color dark = new Color(0.12f, 0.07f, 0.05f);

            CreatePart("ToriiPost", PrimitiveType.Cylinder, torii.transform,
                new Vector3(-2.1f, 2.2f, 0f), new Vector3(0.28f, 2.2f, 0.28f), red);
            CreatePart("ToriiPost", PrimitiveType.Cylinder, torii.transform,
                new Vector3(2.1f, 2.2f, 0f), new Vector3(0.28f, 2.2f, 0.28f), red);
            CreatePart("ToriiBeam", PrimitiveType.Cube, torii.transform,
                new Vector3(0f, 4.05f, 0f), new Vector3(5.7f, 0.34f, 0.45f), red);
            CreatePart("ToriiTop", PrimitiveType.Cube, torii.transform,
                new Vector3(0f, 4.62f, 0f), new Vector3(6.7f, 0.28f, 0.55f), dark,
                new Vector3(0f, 0f, -2f));
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

        private static void ConfigureSky()
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                string skyPath = $"{MaterialFolder}/JapaneseTwilightSky.mat";
                Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
                if (sky == null)
                {
                    sky = new Material(skyShader)
                    {
                        name = "JapaneseTwilightSky"
                    };
                    AssetDatabase.CreateAsset(sky, skyPath);
                }

                sky.SetColor("_SkyTint", new Color(0.32f, 0.46f, 0.67f));
                sky.SetColor("_GroundColor", new Color(0.13f, 0.19f, 0.20f));
                sky.SetFloat("_AtmosphereThickness", 0.72f);
                sky.SetFloat("_SunSize", 0.045f);
                sky.SetFloat("_SunSizeConvergence", 4.5f);
                sky.SetFloat("_Exposure", 1.08f);
                RenderSettings.skybox = sky;
                EditorUtility.SetDirty(sky);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.26f, 0.36f, 0.43f);
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.reflectionIntensity = 0.65f;
            RenderSettings.haloStrength = 0.3f;
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
