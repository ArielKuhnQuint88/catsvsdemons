using System.Collections.Generic;
using CatsVsDemons.Defense;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Visuals
{
    [DefaultExecutionOrder(-50)]
    public sealed class GardenArtDirector : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new();
        private readonly List<Material> materials = new();
        private readonly List<Vector3[]> routes = new();
        private readonly List<Vector3> buildPositions = new();
        private Transform scenery;
        private Transform buildSpots;
        private Material sceneMaterial;
        private Material terrainMaterial;
        private Material roadMaterial;
        private Material edgeMaterial;
        private Material waterMaterial;
        private Material roofMaterial;
        private Material timberMaterial;
        private Material sky;
        private Material previousSky;
        private int scenario;
        private int season;
        private bool pending;
        private Palette palette;

        private struct Palette
        {
            public Color Ground, DeepGreen, Leaves, Blossom, Stone, Wood, Roof, Paper, Gold, Water, Fog, Sky;
        }

        public void Schedule(int sceneIndex, int seasonIndex, IReadOnlyList<Vector3[]> worldRoutes, Transform spots)
        {
            scenario = sceneIndex; season = seasonIndex; buildSpots = spots;
            routes.Clear();
            foreach (Vector3[] route in worldRoutes) routes.Add(route);
            pending = true;
        }

        private void LateUpdate()
        {
            if (!pending) return;
            pending = false;
            Rebuild();
        }

        private static Color C(uint value) => new(((value >> 16) & 255) / 255f,
            ((value >> 8) & 255) / 255f, (value & 255) / 255f);

        private Palette CreatePalette()
        {
            Palette p = new()
            {
                Ground = C(0x6B8968), DeepGreen = C(0x344E44), Leaves = C(0x7D9F72),
                Blossom = C(0xE4B4BD), Stone = C(0xA19E8B), Wood = C(0x725342),
                Roof = C(0x344D50), Paper = C(0xEFE0B8), Gold = C(0xC6A05B),
                Water = C(0x357C7C), Fog = C(0xAFC8C4), Sky = C(0xA8C6D4)
            };
            if (season == 1) { p.Leaves = C(0x638864); p.Blossom = C(0xB9C9A0); }
            if (season == 2) { p.Leaves = C(0xBE9863); p.Blossom = C(0xC87C63); p.Ground = C(0x808660); }
            if (season == 3)
            {
                p.Ground = C(0xCAD8CA); p.Leaves = C(0x81978B); p.Blossom = C(0xDDE4DF);
                p.Fog = C(0xC8D6DC); p.Sky = C(0xBECFD9); p.Stone = C(0xB6BEBA);
            }
            if (scenario == 1)
            {
                p.Ground = Color.Lerp(p.Ground, C(0x4D7664), 0.35f);
                p.DeepGreen = C(0x254D45); p.Roof = C(0x3A5953);
                p.Fog = Color.Lerp(p.Fog, C(0xAFCEC7), 0.32f);
            }
            if (scenario == 2)
            {
                p.Ground = Color.Lerp(p.Ground, C(0x526E79), 0.38f);
                p.Leaves = Color.Lerp(p.Leaves, C(0x7C9DB0), 0.3f);
                p.Blossom = Color.Lerp(p.Blossom, C(0xB7B2CF), 0.5f);
                p.Roof = C(0x394F68); p.Stone = C(0x8F9BAA); p.Water = C(0x426F89);
                p.Fog = C(0x9CAFC9); p.Sky = C(0x7E91B4);
            }
            return p;
        }

        private void Rebuild()
        {
            ClearScenery();
            palette = CreatePalette();
            HideOldDecorations();
            scenery = new GameObject("Art Directed Japanese Garden").transform;
            scenery.SetParent(transform, false);
            sceneMaterial = MakeMaterial("Garden architecture and plants");
            roofMaterial = MakeMaterial("Glazed roof tile detail", 2f);
            timberMaterial = MakeMaterial("Warm timber grain", 3f);
            terrainMaterial = MakeMaterial("Painted moss terrain");
            Texture2D moss = Resources.Load<Texture2D>("Textures/Environment/PaintedMoss");
            if (moss != null) terrainMaterial.SetTexture("_BaseMap", moss);
            terrainMaterial.SetFloat("_UseTexture", moss != null ? 1f : 0f);
            terrainMaterial.SetFloat("_WindStrength", 0f);
            terrainMaterial.SetFloat("_DetailScale", 0.075f);
            roadMaterial = MakeMaterial("Warm weathered stone paving");
            roadMaterial.SetColor("_BaseColor", Color.Lerp(palette.Stone, palette.Paper, 0.12f));
            roadMaterial.SetFloat("_Pattern", 1f);
            roadMaterial.SetFloat("_WindStrength", 0f);
            edgeMaterial = MakeMaterial("Path moss and stone edge");
            edgeMaterial.SetColor("_BaseColor", Color.Lerp(palette.Stone, palette.DeepGreen, 0.5f));
            edgeMaterial.SetFloat("_WindStrength", 0f);
            Shader waterShader = Shader.Find("CatsVsDemons/GardenWater");
            if (waterShader != null)
            {
                waterMaterial = new Material(waterShader) { name = "Garden reflecting pools" };
                waterMaterial.SetColor("_BaseColor", palette.Water);
                waterMaterial.SetColor("_HighlightColor", Color.Lerp(palette.Water, palette.Paper, 0.6f));
                materials.Add(waterMaterial);
            }
            buildPositions.Clear();
            if (buildSpots != null) foreach (Transform spot in buildSpots) buildPositions.Add(spot.position);
            ApplyPathMaterials();
            BuildTerrain();
            BuildMountains();
            BuildArchitecture();
            BuildVegetation();
            BuildPonds();
            HarmonizeBuildSpots();
            ApplyAtmosphere();
        }

        private Material MakeMaterial(string name, float pattern = 0f)
        {
            Shader shader = Shader.Find("CatsVsDemons/GardenScenery");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new(shader) { name = name };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Pattern", pattern);
            materials.Add(material);
            return material;
        }

        private void Emit(string name, GardenMeshBuilder builder, Material material = null, bool castShadow = true)
        {
            if (builder.VertexCount == 0) return;
            GameObject part = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(scenery, false);
            Mesh mesh = builder.Build(name);
            meshes.Add(mesh);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material != null ? material : sceneMaterial;
            renderer.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        private void HideOldDecorations()
        {
            Transform environment = GameObject.Find("Game/Environment")?.transform;
            if (environment != null)
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                bool healthDisplay = false;
                for (Transform node = renderer.transform; node != environment && node != null; node = node.parent)
                    if (node.name.Contains("Health")) { healthDisplay = true; break; }
                if (!healthDisplay) renderer.enabled = false;
            }
            Transform legacy = GameObject.Find("Game/RuntimeGarden")?.transform;
            if (legacy != null) foreach (Renderer renderer in legacy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            GameObject horizon = GameObject.Find("Runtime Horizon Ground");
            if (horizon != null && horizon.TryGetComponent(out Renderer ground)) ground.enabled = false;
        }

        private void ApplyPathMaterials()
        {
            Transform root = GameObject.Find("Game/Paths")?.transform;
            if (root == null) return;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = renderer.name;
                if (name.EndsWith("_Surface")) renderer.sharedMaterial = roadMaterial;
                else if (name.EndsWith("_Border")) renderer.sharedMaterial = edgeMaterial;
            }
        }

        private void BuildTerrain()
        {
            GardenMeshBuilder ground = new();
            const int columns = 52, rows = 44;
            Vector3 Point(int x, int z)
            {
                float px = -82f + x * 164f / columns;
                float pz = -68f + z * 142f / rows;
                float outer = Mathf.Clamp01(Mathf.Max(Mathf.Abs(px) - 23f, Mathf.Abs(pz) - 19f) / 16f);
                float height = outer * (0.4f + Mathf.PerlinNoise(px * 0.04f + 20f, pz * 0.04f + 20f) * 3.4f);
                float road = GardenPathGeometry.DistanceToRoutes(new Vector3(px, 0, pz), routes);
                // Leave full grid triangles beneath even the outer path edges.
                height *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((road - 6.5f) / 4f));
                return new Vector3(px, height + 0.015f, pz);
            }
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < columns; x++)
            {
                Vector3 center = Point(x, z);
                float variation = Mathf.PerlinNoise(center.x * 0.1f + 50f, center.z * 0.1f + 50f);
                Color tint = Color.Lerp(palette.Ground, palette.DeepGreen, variation * 0.26f);
                ground.Quad(center, Point(x, z + 1), Point(x + 1, z + 1), Point(x + 1, z), tint);
            }
            Emit("Organic painted terrain", ground, terrainMaterial, false);
        }

        private void BuildMountains()
        {
            for (int layer = 0; layer < 3; layer++)
            {
                GardenMeshBuilder mountains = new();
                float radiusX = 67f + layer * 34f;
                float radiusZ = 57f + layer * 31f;
                Color tint = Color.Lerp(palette.DeepGreen, palette.Fog, 0.48f + layer * 0.18f);
                const int slices = 80;
                Vector3 Point(int slice, int band)
                {
                    float angle = slice * Mathf.PI * 2f / slices;
                    float curve = 0.7f + 0.22f * Mathf.Sin(angle * 7f + layer) +
                        0.17f * Mathf.Cos(angle * 13f + layer * 2f);
                    float factor = band == 0 ? 0.76f : band == 1 ? 1f : 1.25f;
                    float height = band == 1 ? (8f + layer * 2.8f) * curve : -1.8f;
                    return new Vector3(Mathf.Cos(angle) * radiusX * factor,
                        height, Mathf.Sin(angle) * radiusZ * factor + 8f);
                }
                for (int slice = 0; slice < slices; slice++)
                for (int band = 0; band < 2; band++)
                    mountains.Quad(Point(slice, band), Point(slice + 1, band),
                        Point(slice + 1, band + 1), Point(slice, band + 1),
                        Color.Lerp(tint, palette.Fog, band * 0.11f));
                Emit("Mountain range " + layer, mountains, null, false);
            }
        }

        private bool ClearForPlants(Vector3 point, float radius)
        {
            if (Mathf.Abs(point.x) < 6.2f && point.z > -4.5f && point.z < 8f) return false;
            if (GardenPathGeometry.DistanceToRoutes(point, routes) < radius) return false;
            foreach (Vector3 spot in buildPositions) if ((point - spot).sqrMagnitude < 7f) return false;
            foreach (Vector3 pond in new[] { new Vector3(-15f, 0, -9.5f), new Vector3(15f, 0, 9.5f) })
                if ((point - pond).sqrMagnitude < 12f) return false;
            return true;
        }

        private void BuildVegetation()
        {
            GardenMeshBuilder trunks = new(), crowns = new(), undergrowth = new(), rocks = new();
            System.Random random = new(4802 + scenario * 173);
            bool small = Application.isMobilePlatform;
            int treeBudget = small ? 36 : 55;
            int trees = 0;
            foreach (int row in new[] { 11, 2, 9, 0, 7, 4, 10, 1, 8, 3, 6, 5 })
            for (int column = 0; column < 16 && trees < treeBudget; column++)
            {
                Vector3 point = new(-32f + column * 4.2f + (float)random.NextDouble() * 1.2f,
                    0f, -22f + row * 4.5f + (float)random.NextDouble() * 1.2f);
                if (!ClearForPlants(point, 3.7f) || (point.z < -16f && Mathf.Abs(point.x) < 21f)) continue;
                int kind = (column + row * 3) % 5;
                if (scenario == 1 && kind > 1) Bamboo(trunks, crowns, point, 3.8f + (float)random.NextDouble() * 1.6f, column + row);
                else if (kind == 0 || kind == 3) Pine(trunks, crowns, point, 4.2f + (float)random.NextDouble() * 2f);
                else Cherry(trunks, crowns, point, 3.7f + (float)random.NextDouble() * 1.6f, kind, column + row * 11);
                if (++trees >= treeBudget) break;
            }
            // Small plants cluster beside safe route margins instead of hiding enemies.
            int plantBudget = small ? 125 : 210;
            for (int index = 0; index < plantBudget; index++)
            {
                Vector3 point = new((float)random.NextDouble() * 58f - 29f, 0f,
                    (float)random.NextDouble() * 48f - 22f);
                if (!ClearForPlants(point, 2f)) continue;
                if (index % 7 == 0)
                    rocks.Blob(point + Vector3.up * 0.22f, new Vector3(0.9f, 0.5f, 0.65f),
                        Color.Lerp(palette.Stone, palette.DeepGreen, 0.24f), index);
                else if (index % 4 == 0)
                {
                    Color bloom = index % 3 == 0 ? palette.Blossom : Color.Lerp(palette.Gold, palette.Paper, 0.38f);
                    for (int flower = 0; flower < 3; flower++)
                    {
                        Vector3 top = point + new Vector3(flower * 0.18f, 0.3f + flower * 0.1f, 0.1f * (flower % 2));
                        undergrowth.Stem(top - Vector3.up * top.y, top, 0.025f, palette.DeepGreen, 0.015f, 4);
                        undergrowth.Blob(top, new Vector3(0.3f, 0.12f, 0.3f), bloom, flower, 0.5f);
                    }
                }
                else if (index % 3 == 0)
                    undergrowth.Blob(point + Vector3.up * 0.35f, new Vector3(1.2f, 0.7f, 0.8f),
                        Color.Lerp(palette.Leaves, palette.DeepGreen, 0.32f), index, 0.6f);
                else
                {
                    for (int leaf = 0; leaf < 7; leaf++)
                    {
                        float angle = leaf * Mathf.PI * 2f / 7f;
                        Vector3 tip = point + new Vector3(Mathf.Cos(angle) * 0.5f, 0.38f,
                            Mathf.Sin(angle) * 0.5f);
                        undergrowth.Leaf(point + Vector3.up * 0.05f, tip, 0.11f,
                            Color.Lerp(palette.Leaves, palette.DeepGreen, leaf % 2 * 0.35f));
                    }
                }
            }
            // Deliberate small flower beds give the garden recognizable planted areas.
            foreach (Vector3 bed in new[] { new Vector3(-11f, 0, 12f), new Vector3(11f, 0, -11f),
                new Vector3(-20f, 0, -3f), new Vector3(19f, 0, 2f) })
            {
                int flowers = small ? 7 : 12;
                for (int flower = 0; flower < flowers; flower++)
                {
                    float angle = flower * Mathf.PI * 2f / flowers;
                    Vector3 at = bed + new Vector3(Mathf.Cos(angle) * (0.7f + flower % 2 * 0.55f),
                        0f, Mathf.Sin(angle) * (0.7f + flower % 2 * 0.55f));
                    if (!ClearForPlants(at, 2f)) continue;
                    Vector3 top = at + Vector3.up * (0.33f + flower % 3 * 0.08f);
                    undergrowth.Stem(at, top, 0.028f, palette.DeepGreen, 0.015f, 4);
                    undergrowth.Leaf(at + Vector3.up * 0.15f, at + new Vector3(0.3f, 0.28f, 0.14f), 0.08f, palette.Leaves);
                    undergrowth.Blob(top, new Vector3(0.35f, 0.15f, 0.35f),
                        flower % 3 == 0 ? palette.Paper : palette.Blossom, flower, 0.65f);
                }
            }
            Emit("Bark branches and roots", trunks, timberMaterial);
            Emit("Sakura maple pine and bamboo canopy", crowns);
            Emit("Ferns grasses flowers and shrubs", undergrowth);
            Emit("Garden moss rocks", rocks);
        }

        private void Cherry(GardenMeshBuilder wood, GardenMeshBuilder leaves, Vector3 point, float height, int kind, int seed)
        {
            Vector3 top = point + new Vector3(0.25f, height * 0.78f, 0);
            wood.Stem(point, top, 0.18f, palette.Wood, 0.09f);
            Color foliage = kind == 1 ? palette.Blossom : palette.Leaves;
            for (int branch = 0; branch < 4; branch++)
            {
                float a = (branch * 90f + seed * 13f) * Mathf.Deg2Rad;
                Vector3 end = point + new Vector3(Mathf.Cos(a) * 1.25f, height * (0.73f + branch % 2 * 0.12f), Mathf.Sin(a) * 1.25f);
                wood.Stem(top - Vector3.up * 0.6f, end, 0.085f, palette.Wood, 0.035f, 6);
                leaves.Blob(end + Vector3.up * 0.3f, new Vector3(2.5f, 1.6f, 2.25f),
                    Color.Lerp(foliage, palette.Paper, branch * 0.035f), seed + branch, 1f);
            }
            leaves.Blob(top + Vector3.up * 0.9f, new Vector3(2.1f, 1.5f, 2f), foliage, seed, 1f);
        }

        private void Pine(GardenMeshBuilder wood, GardenMeshBuilder leaves, Vector3 point, float height)
        {
            wood.Stem(point, point + Vector3.up * height, 0.16f, palette.Wood, 0.04f);
            Color foliage = Color.Lerp(palette.DeepGreen, palette.Leaves, 0.25f);
            for (int tier = 0; tier < 3; tier++)
            {
                float y = height * (0.25f + tier * 0.2f);
                leaves.Stem(point + Vector3.up * y, point + Vector3.up * (y + height * 0.46f),
                    1.65f - tier * 0.4f, Color.Lerp(foliage, palette.Paper, tier * 0.03f), 0.025f, 10, 0.8f);
            }
        }

        private void Bamboo(GardenMeshBuilder wood, GardenMeshBuilder leaves, Vector3 point, float height, int seed)
        {
            for (int stalk = 0; stalk < 4; stalk++)
            {
                Vector3 start = point + new Vector3(Mathf.Sin(seed + stalk) * 0.55f, 0f, Mathf.Cos(seed + stalk) * 0.55f);
                Vector3 end = start + new Vector3(stalk * 0.07f, height - stalk * 0.3f, 0.2f);
                wood.Stem(start, end, 0.065f, Color.Lerp(palette.Leaves, palette.Gold, 0.25f), 0.045f, 6);
                for (int node = 1; node < 7; node++)
                {
                    Vector3 center = Vector3.Lerp(start, end, node / 7f);
                    if (node % 2 == 0)
                        wood.Box(center, new Vector3(0.14f, 0.055f, 0.14f), palette.DeepGreen);
                    if (node < 4) continue;
                    leaves.Leaf(center, center + new Vector3(0.7f, 0.24f, 0.32f), 0.14f, palette.Leaves);
                    leaves.Leaf(center, center + new Vector3(-0.62f, 0.3f, -0.2f), 0.12f, palette.Leaves);
                }
            }
        }

        private void BuildArchitecture()
        {
            GardenMeshBuilder stone = new(), wood = new(), roofs = new(), paper = new(), accents = new();
            Vector3 home = GameObject.Find("Game/Environment/JapaneseGarden3D/JapaneseHouse")?.transform.position ?? new Vector3(0, 0, 1.8f);
            home.y = 0f;
            GardenMeshBuilder homeStone = new(), homeWood = new(), homeRoof = new(), homePaper = new(), homeTrim = new();
            House(homeStone, homeWood, homeRoof, homePaper, homeTrim, home, 1f);
            // Match the authored house footprint so its eaves do not cover routes.
            Vector3 footprint = new(0.66f, 1f, 0.66f);
            foreach (GardenMeshBuilder group in new[] { homeStone, homeWood, homeRoof, homePaper, homeTrim })
                group.ScaleAround(home, footprint);
            stone.Append(homeStone); wood.Append(homeWood); roofs.Append(homeRoof);
            paper.Append(homePaper); accents.Append(homeTrim);
            Pavilion(stone, wood, roofs, accents, new Vector3(-24f, 0f, 23f), 1.3f);
            Pavilion(stone, wood, roofs, accents, new Vector3(23f, 0f, 27f), 1.05f);
            Pagoda(stone, wood, roofs, paper, accents, new Vector3(12f, 0f, 34f), scenario == 2 ? 1.3f : 1f);
            Torii(stone, wood, accents, new Vector3(-27f, 0f, 8f), 90f);
            Torii(stone, wood, accents, new Vector3(26f, 0f, 10f), 90f);
            Torii(stone, wood, accents, new Vector3(0f, 0f, 29f), 0f);
            // Low rear walls frame the garden while keeping combat sightlines clear.
            for (int side = -1; side <= 1; side += 2)
            for (int panel = 0; panel < 5; panel++)
            {
                Vector3 at = new(side * (9f + panel * 3.4f), 0f, 22f + panel % 2 * 0.3f);
                if (GardenPathGeometry.DistanceToRoutes(at, routes) < 2.6f) continue;
                stone.Box(at + Vector3.up * 0.32f, new Vector3(3.2f, 0.6f, 0.6f), palette.Stone);
                paper.Box(at + Vector3.up * 1.05f, new Vector3(3.1f, 0.85f, 0.24f), palette.Paper);
                roofs.Roof(at + Vector3.up * 1.48f, 3.65f, 0.82f, 0.3f, palette.Roof);
                wood.Box(at + new Vector3(-1.5f, 1.02f, 0), new Vector3(0.14f, 1.2f, 0.38f), palette.Wood);
                wood.Box(at + new Vector3(1.5f, 1.02f, 0), new Vector3(0.14f, 1.2f, 0.38f), palette.Wood);
            }
            foreach (Vector3 at in new[] { new Vector3(-8.7f, 0f, -3f), new Vector3(8.7f, 0f, -3f),
                new Vector3(-20f, 0f, 14f), new Vector3(20f, 0f, 16f) })
                Lantern(stone, roofs, accents, at);
            // Hanging fabric and ridge trims add detail without extra scenery renderers.
            foreach (Vector3 at in new[] { new Vector3(-23f, 0, 20f), new Vector3(22f, 0, 24.5f) })
            {
                wood.Stem(at, at + Vector3.up * 3.1f, 0.075f, palette.Wood, 0.055f, 6);
                wood.Stem(at + Vector3.up * 2.95f, at + new Vector3(1.1f, 2.95f, 0), 0.055f, palette.Wood, 0.045f, 6);
                paper.Quad(at + new Vector3(0.12f, 2.86f, 0), at + new Vector3(0.12f, 1.35f, 0),
                    at + new Vector3(0.97f, 1.35f, 0), at + new Vector3(0.97f, 2.86f, 0), palette.Paper, 0.7f);
                paper.Quad(at + new Vector3(0.12f, 2.86f, 0), at + new Vector3(0.97f, 2.86f, 0),
                    at + new Vector3(0.97f, 1.35f, 0), at + new Vector3(0.12f, 1.35f, 0), palette.Paper, 0.7f);
                accents.Box(at + new Vector3(0.55f, 2.3f, -0.02f), new Vector3(0.35f, 0.4f, 0.06f), palette.Gold);
            }
            Emit("Stone podiums steps and garden walls", stone);
            Emit("Timber beams columns and door frames", wood, timberMaterial);
            Emit("Curved tiled roofs and ridge details", roofs, roofMaterial);
            Emit("Shoji paper screens", paper);
            Emit("Warm lanterns brass and ornaments", accents);
        }

        private void House(GardenMeshBuilder stone, GardenMeshBuilder wood, GardenMeshBuilder roof,
            GardenMeshBuilder paper, GardenMeshBuilder trim, Vector3 at, float size)
        {
            stone.Box(at + Vector3.up * 0.2f, new Vector3(8.7f, 0.4f, 6.7f) * size, palette.Stone);
            wood.Box(at + Vector3.up * 0.48f, new Vector3(8.1f, 0.2f, 6.1f) * size, palette.Wood);
            for (int step = 0; step < 3; step++)
                stone.Box(at + new Vector3(0, 0.07f + step * 0.1f, -3.9f + step * 0.35f),
                    new Vector3(3.6f, 0.14f, 0.6f), palette.Stone);
            paper.Box(at + Vector3.up * 1.9f, new Vector3(7.1f, 2.7f, 4.9f), palette.Paper);
            for (int side = -1; side <= 1; side += 2)
            for (int post = -3; post <= 3; post++)
            {
                wood.Box(at + new Vector3(post * 1.12f, 1.94f, side * 2.5f),
                    new Vector3(0.12f, 2.86f, 0.15f), palette.Wood);
                for (int slat = 0; slat < 3; slat++)
                    wood.Box(at + new Vector3(post * 1.12f, 1.1f + slat * 0.7f, side * 2.52f),
                        new Vector3(1.12f, 0.045f, 0.08f), palette.Wood);
            }
            wood.Box(at + new Vector3(0, 3.28f, 0), new Vector3(7.55f, 0.18f, 5.4f), palette.Wood);
            roof.Roof(at + Vector3.up * 3.28f, 9.4f, 7f, 1.55f, palette.Roof);
            for (int side = -1; side <= 1; side += 2)
                wood.Box(at + new Vector3(0, 3.38f, side * 3.48f), new Vector3(9.2f, 0.1f, 0.12f), palette.Wood);
            wood.Box(at + Vector3.up * 4.76f, new Vector3(2.35f, 0.55f, 1.65f), palette.Wood);
            roof.Roof(at + Vector3.up * 5f, 4f, 2.5f, 0.7f, palette.Roof);
            trim.Stem(at + new Vector3(-1.25f, 5.82f, 0), at + new Vector3(1.25f, 5.82f, 0),
                0.07f, palette.Gold, 0.07f, 6);
            Lantern(stone, roof, trim, at + new Vector3(-3.1f, 0f, -3.05f));
            Lantern(stone, roof, trim, at + new Vector3(3.1f, 0f, -3.05f));
        }

        private void Pavilion(GardenMeshBuilder stone, GardenMeshBuilder wood, GardenMeshBuilder roof,
            GardenMeshBuilder trim, Vector3 at, float size)
        {
            stone.Box(at + Vector3.up * 0.2f, new Vector3(4.4f, 0.4f, 4f) * size, palette.Stone);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                wood.Stem(at + new Vector3(x * 1.55f * size, 0.3f, z * 1.35f * size),
                    at + new Vector3(x * 1.55f * size, 3.35f * size, z * 1.35f * size),
                    0.13f * size, palette.Wood, 0.13f * size);
            roof.Roof(at + Vector3.up * 3.35f * size, 5.3f * size, 4.85f * size, size, palette.Roof);
            trim.Box(at + Vector3.up * 2.55f * size, new Vector3(0.5f, 0.75f, 0.5f) * size, palette.Gold);
            wood.Box(at + new Vector3(0f, 0.7f, 0.9f) * size, new Vector3(2.5f, 0.18f, 0.6f) * size, palette.Wood);
        }

        private void Pagoda(GardenMeshBuilder stone, GardenMeshBuilder wood, GardenMeshBuilder roof,
            GardenMeshBuilder paper, GardenMeshBuilder trim, Vector3 at, float size)
        {
            stone.Box(at + Vector3.up * 0.25f, new Vector3(4.2f, 0.5f, 4.2f) * size, palette.Stone);
            for (int tier = 0; tier < 3; tier++)
            {
                float width = (3.35f - tier * 0.65f) * size;
                float y = (0.5f + tier * 2.05f) * size;
                paper.Box(at + Vector3.up * (y + 0.85f * size), new Vector3(width, 1.7f * size, width), palette.Paper);
                for (int side = -1; side <= 1; side += 2)
                    wood.Box(at + new Vector3(side * width * 0.47f, y + 0.85f * size, 0),
                        new Vector3(0.14f * size, 1.9f * size, width + 0.2f), palette.Wood);
                roof.Roof(at + Vector3.up * (y + 1.6f * size), width + 1.4f * size,
                    width + 1.4f * size, 0.75f * size, palette.Roof);
            }
            trim.Stem(at + Vector3.up * 6.7f * size, at + Vector3.up * 7.8f * size,
                0.11f * size, palette.Gold, 0.025f * size, 7);
        }

        private void Torii(GardenMeshBuilder stone, GardenMeshBuilder wood, GardenMeshBuilder trim, Vector3 at, float yaw)
        {
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            Color lacquer = Color.Lerp(C(0xA55445), palette.Wood, scenario == 2 ? 0.65f : 0.25f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 post = at + turn * new Vector3(side * 2.25f, 0, 0);
                stone.Box(post + Vector3.up * 0.2f, new Vector3(0.8f, 0.4f, 0.8f), palette.Stone);
                wood.Stem(post, post + Vector3.up * 4.45f, 0.18f, lacquer, 0.14f, 8);
            }
            wood.Box(at + Vector3.up * 3.7f, new Vector3(5.4f, 0.22f, 0.35f), lacquer, turn);
            wood.Box(at + Vector3.up * 4.4f, new Vector3(6.25f, 0.24f, 0.52f), palette.Roof, turn);
            wood.Box(at + new Vector3(0, 4.18f, 0), new Vector3(0.55f, 0.68f, 0.12f), lacquer, turn);
            trim.Box(at + new Vector3(0, 4.2f, 0), new Vector3(0.3f, 0.38f, 0.15f), palette.Gold, turn);
        }

        private void Lantern(GardenMeshBuilder stone, GardenMeshBuilder roof, GardenMeshBuilder glow, Vector3 at)
        {
            stone.Box(at + Vector3.up * 0.13f, new Vector3(0.58f, 0.26f, 0.58f), palette.Stone);
            stone.Stem(at + Vector3.up * 0.2f, at + Vector3.up * 1.2f, 0.12f, palette.Stone, 0.1f, 6);
            glow.Box(at + Vector3.up * 1.38f, new Vector3(0.5f, 0.45f, 0.5f), Color.Lerp(palette.Gold, palette.Paper, 0.55f));
            roof.Roof(at + Vector3.up * 1.59f, 0.82f, 0.82f, 0.35f, palette.Roof);
        }

        private void BuildPonds()
        {
            GardenMeshBuilder shore = new(), water = new(), deck = new(), rails = new(), koi = new();
            foreach (Vector3 at in new[] { new Vector3(-15f, 0, -9.5f), new Vector3(15f, 0, 9.5f) })
            {
                for (int side = 0; side < 24; side++)
                {
                    float a = side * Mathf.PI * 2f / 24f;
                    float b = (side + 1) * Mathf.PI * 2f / 24f;
                    Vector3 p0 = at + new Vector3(Mathf.Cos(a) * 2.75f, 0.042f, Mathf.Sin(a) * 1.65f);
                    Vector3 p1 = at + new Vector3(Mathf.Cos(b) * 2.75f, 0.042f, Mathf.Sin(b) * 1.65f);
                    water.Triangle(at + Vector3.up * 0.042f, p1, p0, Color.white);
                    shore.Blob(at + new Vector3(Mathf.Cos(a) * 2.9f, 0.14f, Mathf.Sin(a) * 1.78f),
                        new Vector3(0.6f, 0.36f, 0.4f), Color.Lerp(palette.Stone, palette.DeepGreen, 0.24f), side);
                }
                for (int plank = 0; plank < 10; plank++)
                    deck.Box(at + new Vector3(0, 0.12f, -1.65f + plank * 0.36f),
                        new Vector3(1.9f, 0.05f, 0.33f), Color.Lerp(palette.Wood, palette.Paper, 0.25f));
                for (int side = -1; side <= 1; side += 2)
                {
                    rails.Box(at + new Vector3(side, 0.78f, 0), new Vector3(0.09f, 0.1f, 3.7f), palette.Wood);
                    for (int post = -1; post <= 1; post++)
                        rails.Box(at + new Vector3(side, 0.46f, post * 1.5f), new Vector3(0.1f, 0.75f, 0.1f), palette.Wood);
                }
                for (int fish = 0; fish < 4; fish++)
                {
                    Vector3 p = at + new Vector3(-1.7f + fish * 1.1f, 0.048f, fish % 2 == 0 ? 0.7f : -0.6f);
                    koi.Blob(p, new Vector3(0.32f, 0.035f, 0.12f), fish % 2 == 0 ? C(0xCC8254) : palette.Paper, fish);
                }
                for (int lily = 0; lily < 4; lily++)
                {
                    Vector3 p = at + new Vector3(lily < 2 ? -1.8f : 1.8f, 0.065f, (lily % 2) * 1.4f - 0.7f);
                    koi.Blob(p, new Vector3(0.46f, 0.04f, 0.42f), palette.Leaves, lily);
                    if (lily % 2 == 0)
                        koi.Blob(p + Vector3.up * 0.07f, new Vector3(0.24f, 0.14f, 0.24f), palette.Blossom, lily);
                }
            }
            Emit("Natural pond stone shores", shore);
            Emit("Reflecting garden pools", water, waterMaterial, false);
            Emit("Timber bridge planks", deck);
            Emit("Bridge rails", rails);
            Emit("Koi details", koi, null, false);
        }

        private void HarmonizeBuildSpots()
        {
            if (buildSpots == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            Material material = new(shader) { name = "Carved build platforms" };
            material.SetColor("_BaseColor", Color.Lerp(palette.Stone, palette.Paper, 0.35f));
            material.SetFloat("_Smoothness", 0.15f);
            materials.Add(material);
            foreach (Transform spot in buildSpots)
            {
                // A constructed defense lives below the spot; keep its model and health materials.
                if (spot.TryGetComponent(out MeshRenderer renderer)) renderer.sharedMaterial = material;
            }
        }

        private void ApplyAtmosphere()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = palette.Sky;
                camera.farClipPlane = 550f;
            }
            if (sky == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader != null)
                {
                    previousSky = RenderSettings.skybox;
                    sky = new Material(shader) { name = "Soft Japanese garden sky" };
                }
            }
            if (sky != null)
            {
                sky.SetColor("_SkyTint", palette.Sky);
                sky.SetColor("_GroundColor", palette.Fog);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 0.8f);
                sky.SetFloat("_Exposure", scenario == 2 ? 0.85f : 1.05f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = palette.Fog;
            RenderSettings.fogStartDistance = 65f;
            RenderSettings.fogEndDistance = 220f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(palette.Sky, Color.white, 0.25f);
            RenderSettings.ambientEquatorColor = Color.Lerp(palette.Fog, palette.Paper, 0.24f);
            RenderSettings.ambientGroundColor = C(0x59665C);
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
                sun.color = scenario == 2 ? C(0xD4E0FF) : season == 2 ? C(0xFFE9CE) : C(0xFFF0DA);
                sun.intensity = scenario == 2 ? 0.9f : 1.02f;
                sun.shadowStrength = 0.58f;
            }
        }

        private void ClearScenery()
        {
            if (scenery != null) { scenery.gameObject.SetActive(false); Destroy(scenery.gameObject); }
            foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
            foreach (Material material in materials) if (material != null) Destroy(material);
            meshes.Clear(); materials.Clear();
        }

        private void OnDestroy()
        {
            ClearScenery();
            if (sky != null)
            {
                if (RenderSettings.skybox == sky) RenderSettings.skybox = previousSky;
                Destroy(sky);
            }
        }
    }
}
