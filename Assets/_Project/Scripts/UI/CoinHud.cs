using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using CatsVsDemons.Player;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsVsDemons.UI
{
    public sealed class CoinHud : MonoBehaviour
    {
        private Wallet wallet;
        private HouseHealth house;
        private KinHealth kin;
        private EnemyWaveSpawner waves;
        private GUIStyle coinStyle;
        private GUIStyle healthStyle;
        private GUIStyle helpStyle;
        private GUIStyle resultStyle;
        private GUIStyle messageStyle;
        private GUIStyle countdownStyle;
        private GUIStyle compactStyle;
        private GUIStyle compactCenterStyle;
        private GUIStyle radialStyle;
        private GUIStyle costStyle;
        private Texture2D circleTexture;
        private Texture2D paperTexture;
        private Font orientalFont;
        private Texture2D victoryTexture;
        private Texture2D defeatTexture;
        private Texture2D portalIcon;
        private Texture2D bonsaiIcon;
        private Texture2D lanternIcon;
        private int activeEnemies;
        private float nextEnemyRefresh;
        private int currentPhase;
        private int totalPhases;
        private int currentWave;
        private int totalWaves;
        private int preparationSeconds;
        private bool preparing;
        private bool paused;
        private bool gameOver;
        private bool kinDown;
        private bool victory;

        private void Awake()
        {
            wallet = Object.FindFirstObjectByType<Wallet>();
            house = Object.FindFirstObjectByType<HouseHealth>();
            kin = Object.FindFirstObjectByType<KinHealth>();
            waves = Object.FindFirstObjectByType<EnemyWaveSpawner>();
            orientalFont = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

            coinStyle = CreateStyle(
                28, new Color(1f, 0.82f, 0.16f), FontStyle.Bold
            );
            healthStyle = CreateStyle(
                22, new Color(0.35f, 1f, 0.45f), FontStyle.Bold
            );
            helpStyle = CreateStyle(17, Color.white, FontStyle.Normal);
            resultStyle = CreateStyle(52, Color.white, FontStyle.Bold);
            resultStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle = CreateStyle(22, Color.white, FontStyle.Normal);
            messageStyle.alignment = TextAnchor.MiddleCenter;
            countdownStyle = CreateStyle(
                72,
                new Color(1f, 0.84f, 0.05f),
                FontStyle.Bold
            );
            countdownStyle.alignment = TextAnchor.MiddleCenter;
            compactStyle = CreateStyle(17, Color.white, FontStyle.Bold);
            compactCenterStyle = CreateStyle(16, Color.white, FontStyle.Bold);
            compactCenterStyle.alignment = TextAnchor.MiddleCenter;
            radialStyle = CreateStyle(11, Color.white, FontStyle.Bold);
            radialStyle.alignment = TextAnchor.MiddleCenter;
            costStyle = CreateStyle(
                14,
                new Color(1f, 0.82f, 0.16f),
                FontStyle.Bold
            );
            costStyle.alignment = TextAnchor.MiddleCenter;
            ApplyOrientalFont();
            circleTexture = CreateCircleTexture(128);
            paperTexture = CreatePaperTexture(256);
            victoryTexture = LoadEndingTexture("EndingVictory");
            defeatTexture = LoadEndingTexture("EndingDefeat");
            portalIcon = LoadEndingTexture("TowerPortal");
            bonsaiIcon = LoadEndingTexture("TowerBonsai");
            lanternIcon = LoadEndingTexture("TowerLantern");
        }

        private void Update()
        {
            if (Time.unscaledTime < nextEnemyRefresh)
            {
                return;
            }

            nextEnemyRefresh = Time.unscaledTime + 0.35f;
            activeEnemies = Object.FindObjectsByType<EnemyHealth>(
                FindObjectsSortMode.None
            ).Length;
        }

        private void Start()
        {
            if (house != null)
            {
                house.Destroyed += HandleGameOver;
            }

            if (kin != null)
            {
                kin.Downed += HandleKinDown;
            }

            if (waves != null)
            {
                currentPhase = waves.CurrentPhase;
                totalPhases = waves.TotalPhases;
                currentWave = waves.CurrentWave;
                totalWaves = waves.TotalWaves;
                waves.PhaseStarted += HandlePhaseStarted;
                waves.WaveStarted += HandleWaveStarted;
                waves.PreparationChanged += HandlePreparation;
                waves.PreparationEnded += HandlePreparationEnded;
                waves.Victory += HandleVictory;
            }
        }

        private void OnDestroy()
        {
            if (house != null)
            {
                house.Destroyed -= HandleGameOver;
            }

            if (kin != null)
            {
                kin.Downed -= HandleKinDown;
            }

            if (waves != null)
            {
                waves.PhaseStarted -= HandlePhaseStarted;
                waves.WaveStarted -= HandleWaveStarted;
                waves.PreparationChanged -= HandlePreparation;
                waves.PreparationEnded -= HandlePreparationEnded;
                waves.Victory -= HandleVictory;
            }

            Time.timeScale = 1f;
            if (circleTexture != null)
            {
                Destroy(circleTexture);
            }
            if (paperTexture != null)
            {
                Destroy(paperTexture);
            }
        }

        private void OnGUI()
        {
            if (orientalFont != null)
            {
                GUI.skin.button.font = orientalFont;
            }

            DrawStatusPanel();

            if (preparing && !paused && !gameOver && !kinDown && !victory)
            {
                DrawCountdown();
            }

            if (!gameOver && !kinDown && !victory)
            {
                DrawPauseButton();
            }

            if (gameOver)
            {
                DrawResult(
                    "A CASA CAIU!",
                    "Mesmo ferido, Kin fez tudo o que pôde.",
                    defeatTexture
                );
            }
            else if (kinDown)
            {
                DrawResult(
                    "KIN FOI DERROTADO!",
                    "Seu dono nunca saberá como Kin tentou protegê-lo.",
                    defeatTexture
                );
            }
            else if (victory)
            {
                DrawResult(
                    "A CASA ESTÁ SEGURA!",
                    "Sem conhecer a batalha, seu dono recompensa o melhor gato do mundo.",
                    victoryTexture
                );
            }
            else if (paused)
            {
                DrawPauseMenu();
            }
        }

        private void DrawStatusPanel()
        {
            int coins = wallet != null ? wallet.Coins : 0;
            int houseHealth = house != null ? house.CurrentHealth : 0;
            int houseMax = house != null ? house.MaxHealth : 0;
            int kinHealth = kin != null ? kin.CurrentHealth : 0;
            int kinMax = kin != null ? kin.MaxHealth : 0;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.72f, 1.2f);
            float width = 460f * scale;
            float height = 116f * scale;
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                16f * scale,
                width,
                height
            );

            Rect interfaceBase = new Rect(
                panel.x - 18f * scale,
                panel.y - 8f * scale,
                panel.width + 36f * scale,
                230f * scale
            );
            DrawTexture(interfaceBase, new Color(0.18f, 0.09f, 0.035f, 0.97f));
            Rect paper = new Rect(
                interfaceBase.x + 5f * scale,
                interfaceBase.y + 5f * scale,
                interfaceBase.width - 10f * scale,
                interfaceBase.height - 10f * scale
            );
            DrawPaper(paper);
            DrawTexture(
                new Rect(paper.x, paper.y, paper.width, 3f * scale),
                new Color(0.42f, 0.18f, 0.055f, 0.9f)
            );
            DrawTexture(
                new Rect(paper.x, paper.yMax - 3f * scale, paper.width, 3f * scale),
                new Color(0.42f, 0.18f, 0.055f, 0.9f)
            );

            DrawTexture(panel, new Color(0.24f, 0.12f, 0.045f, 0.10f));
            DrawTexture(
                new Rect(panel.x, panel.y, panel.width, 4f * scale),
                new Color(0.88f, 0.47f, 0.12f, 1f)
            );

            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 10f, 105f, 24f),
                "♥  CASA",
                compactStyle
            );
            Rect houseBar = new Rect(
                panel.x + 112f,
                panel.y + 12f,
                panel.width - 126f,
                20f * scale
            );
            DrawBar(
                houseBar,
                houseMax > 0 ? (float)houseHealth / houseMax : 0f,
                new Color(0.92f, 0.18f, 0.12f),
                $"{houseHealth}/{houseMax}"
            );

            Rect kinBar = new Rect(
                panel.x + 112f,
                panel.y + 39f * scale,
                panel.width - 126f,
                12f * scale
            );
            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 34f * scale, 92f, 24f),
                "KIN",
                compactStyle
            );
            DrawBar(
                kinBar,
                kinMax > 0 ? (float)kinHealth / kinMax : 0f,
                HealthColor(kinHealth, kinMax),
                string.Empty
            );

            float rowY = panel.y + 66f * scale;
            float cellWidth = panel.width / 4f;
            DrawStatusCell(
                new Rect(panel.x, rowY, cellWidth, 34f * scale),
                $"●  {coins}",
                new Color(1f, 0.78f, 0.12f)
            );
            DrawStatusCell(
                new Rect(panel.x + cellWidth, rowY, cellWidth, 34f * scale),
                $"FASE {currentPhase}/{totalPhases}",
                new Color(0.56f, 0.82f, 1f)
            );
            DrawStatusCell(
                new Rect(panel.x + cellWidth * 2f, rowY, cellWidth, 34f * scale),
                $"ONDA {currentWave}/{totalWaves}",
                new Color(0.82f, 0.68f, 1f)
            );
            DrawStatusCell(
                new Rect(panel.x + cellWidth * 3f, rowY, cellWidth, 34f * scale),
                $"☠  {activeEnemies}",
                new Color(0.92f, 0.35f, 1f)
            );

            Vector2 anchor = new Vector2(
                Screen.width * 0.5f,
                panel.yMax + 46f * scale
            );
            float radius = 34f * scale;
            DrawDefenseButton(
                anchor + new Vector2(-88f * scale, 0f),
                radius,
                "PORTAL",
                10,
                DefenseType.Portal,
                new Color(0.04f, 0.48f, 0.95f),
                portalIcon
            );
            DrawDefenseButton(
                anchor,
                radius,
                "BONSAI",
                15,
                DefenseType.Bonsai,
                new Color(0.1f, 0.62f, 0.24f),
                bonsaiIcon
            );
            DrawDefenseButton(
                anchor + new Vector2(88f * scale, 0f),
                radius,
                "LANTERNA",
                10,
                DefenseType.Lantern,
                new Color(0.68f, 0.45f, 0.82f),
                lanternIcon
            );
        }

        private void DrawDefenseButton(
            Vector2 center,
            float radius,
            string label,
            int cost,
            DefenseType type,
            Color color,
            Texture2D icon)
        {
            bool selected = TowerBuildSelection.Selected == type;
            bool hovered = Vector2.Distance(Event.current.mousePosition, center) <= radius;
            float outerRadius = selected ? radius + 6f : radius + 3f;
            DrawCircle(
                center,
                outerRadius,
                selected
                    ? new Color(1f, 0.73f, 0.18f, 0.98f)
                    : new Color(0.02f, 0.05f, 0.08f, 0.92f)
            );
            DrawCircle(
                center,
                radius,
                hovered ? Color.Lerp(color, Color.white, 0.2f) : color
            );
            DrawCircle(center, radius * 0.82f, new Color(0.025f, 0.07f, 0.1f, 0.9f));

            if (icon != null)
            {
                float iconSize = radius *
                    (type == DefenseType.Lantern ? 2.32f : 1.92f);
                GUI.DrawTexture(
                    new Rect(
                        center.x - iconSize * 0.5f,
                        center.y - iconSize * 0.5f,
                        iconSize,
                        iconSize
                    ),
                    icon,
                    ScaleMode.ScaleAndCrop
                );
            }

            GUI.Label(
                new Rect(center.x - radius, center.y + radius - 7f,
                    radius * 2f, 17f),
                label,
                radialStyle
            );
            GUI.Label(
                new Rect(center.x - radius, center.y + radius + 8f,
                    radius * 2f, 20f),
                $"● {cost}",
                costStyle
            );

            if (hovered && Event.current.type == EventType.MouseUp &&
                Event.current.button == 0)
            {
                TowerBuildSelection.Select(type);
                Event.current.Use();
            }
        }

        private void DrawStatusCell(Rect area, string text, Color color)
        {
            Color previous = compactCenterStyle.normal.textColor;
            compactCenterStyle.normal.textColor = Color.black;
            GUI.Label(area, text, compactCenterStyle);
            compactCenterStyle.normal.textColor = previous;
        }

        private void DrawBar(Rect area, float amount, Color color, string text)
        {
            DrawTexture(area, new Color(0.015f, 0.025f, 0.04f, 1f));
            Rect fill = area;
            fill.width = Mathf.Max(0f, area.width * Mathf.Clamp01(amount));
            fill.x += 2f;
            fill.y += 2f;
            fill.height = Mathf.Max(1f, fill.height - 4f);
            fill.width = Mathf.Max(0f, fill.width - 4f);
            DrawTexture(fill, color);
            if (!string.IsNullOrEmpty(text))
            {
                GUI.Label(area, text, compactCenterStyle);
            }
        }

        private Vector2 GetHouseAnchor(float scale)
        {
            Vector2 fallback = new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.55f
            );
            Camera camera = Camera.main;
            if (house == null || camera == null)
            {
                return fallback;
            }

            Vector3 point = camera.WorldToScreenPoint(
                house.transform.position + Vector3.up * 4.5f
            );
            if (point.z <= 0f)
            {
                return fallback;
            }

            return new Vector2(
                Mathf.Clamp(point.x, 150f * scale, Screen.width - 150f * scale),
                Mathf.Clamp(Screen.height - point.y + 10f * scale,
                    Screen.height * 0.24f,
                    Screen.height - 145f * scale)
            );
        }

        private void DrawCircle(Vector2 center, float radius, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    center.x - radius,
                    center.y - radius,
                    radius * 2f,
                    radius * 2f
                ),
                circleTexture,
                ScaleMode.StretchToFill,
                true
            );
            GUI.color = previous;
        }

        private static void DrawTexture(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawPaper(Rect area)
        {
            if (paperTexture == null)
            {
                DrawTexture(area, new Color(0.89f, 0.79f, 0.59f, 0.98f));
                return;
            }

            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.94f, 0.78f, 0.98f);
            GUI.DrawTextureWithTexCoords(
                area,
                paperTexture,
                new Rect(0f, 0f, area.width / 128f, area.height / 128f),
                true
            );
            GUI.color = previous;
        }

        private void ApplyOrientalFont()
        {
            if (orientalFont == null)
            {
                return;
            }

            GUIStyle[] styles =
            {
                coinStyle, healthStyle, helpStyle, resultStyle, messageStyle,
                countdownStyle, compactStyle, compactCenterStyle, radialStyle,
                costStyle
            };
            foreach (GUIStyle style in styles)
            {
                style.font = orientalFont;
            }

            Color ink = Color.black;
            compactStyle.normal.textColor = ink;
            compactCenterStyle.normal.textColor = ink;
            radialStyle.normal.textColor = ink;
            costStyle.normal.textColor = ink;
        }

        private static Texture2D CreatePaperTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HudPaperTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.075f, y * 0.075f);
                    float fiber = Mathf.Sin(y * 0.31f + x * 0.025f) * 0.018f;
                    float shade = Mathf.Lerp(-0.065f, 0.055f, grain) + fiber;
                    pixels[y * size + x] = new Color(
                        0.86f + shade,
                        0.73f + shade,
                        0.49f + shade * 0.65f,
                        1f
                    );
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color HealthColor(int current, int maximum)
        {
            float amount = maximum > 0 ? (float)current / maximum : 0f;
            if (amount > 0.6f)
            {
                return new Color(0.16f, 0.8f, 0.3f);
            }
            return amount > 0.3f
                ? new Color(1f, 0.72f, 0.08f)
                : new Color(0.92f, 0.12f, 0.08f);
        }

        private static Texture2D LoadEndingTexture(string name)
        {
            TextAsset encoded = Resources.Load<TextAsset>($"UI/{name}Data");
            if (encoded != null)
            {
                Texture2D decoded = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                )
                {
                    name = $"{name}_Runtime"
                };
                if (ImageConversion.LoadImage(decoded, encoded.bytes, true))
                {
                    return decoded;
                }

                Object.Destroy(decoded);
            }

            Texture2D imported = Resources.Load<Texture2D>($"UI/{name}");
            if (imported != null)
            {
                return imported;
            }

            Debug.LogError($"Artwork was not found or decoded: {name}");
            return null;
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            )
            {
                name = "RuntimeHudCircle",
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 solid = new Color32(255, 255, 255, 255);
            float center = (size - 1) * 0.5f;
            float radius = center - 1f;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        new Vector2(center, center)
                    );
                    pixels[y * size + x] = distance <= radius ? solid : clear;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void DrawCountdown()
        {
            GUI.Label(
                new Rect(
                    0f,
                    (Screen.height - 220f) * 0.5f,
                    Screen.width,
                    220f
                ),
                $"FASE {currentPhase}\n{preparationSeconds}",
                countdownStyle
            );
        }

        private void DrawResult(
            string title,
            string message,
            Texture2D background)
        {
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            if (background != null)
            {
                GUI.DrawTexture(
                    screen,
                    background,
                    ScaleMode.ScaleAndCrop
                );
            }
            else
            {
                GUI.Box(screen, GUIContent.none);
            }

            DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height * 0.25f),
                new Color(0.01f, 0.02f, 0.04f, 0.78f)
            );
            DrawTexture(
                new Rect(0f, Screen.height * 0.73f,
                    Screen.width, Screen.height * 0.27f),
                new Color(0.01f, 0.02f, 0.04f, 0.84f)
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.04f, Screen.width, 90f),
                title,
                resultStyle
            );
            GUI.Label(
                new Rect(
                    Screen.width * 0.1f,
                    Screen.height * 0.76f,
                    Screen.width * 0.8f,
                    60f
                ),
                message,
                messageStyle
            );

            if (GUI.Button(
                new Rect(
                    (Screen.width - 220f) * 0.5f,
                    Screen.height * 0.88f,
                    220f,
                    52f
                ),
                "Reiniciar"))
            {
                RestartGame();
            }
        }

        private void DrawPauseButton()
        {
            if (GUI.Button(
                new Rect(Screen.width - 130f, 22f, 105f, 42f),
                paused ? "Continuar" : "Pausar"))
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void DrawPauseMenu()
        {
            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.34f, Screen.width, 90f),
                "PAUSADO",
                resultStyle
            );

            if (GUI.Button(
                new Rect(
                    (Screen.width - 220f) * 0.5f,
                    Screen.height * 0.5f,
                    220f,
                    52f
                ),
                "Continuar"))
            {
                paused = false;
                Time.timeScale = 1f;
            }

            if (GUI.Button(
                new Rect(
                    (Screen.width - 220f) * 0.5f,
                    Screen.height * 0.5f + 66f,
                    220f,
                    52f
                ),
                "Reiniciar"))
            {
                RestartGame();
            }
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex < 0)
            {
                Debug.LogError(
                    "Execute Tools/Cats vs Demons/Prepare Playable Build."
                );
                return;
            }

            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void HandlePhaseStarted(int phase, int total)
        {
            currentPhase = phase;
            totalPhases = total;
            currentWave = 0;
        }

        private void HandleWaveStarted(int wave, int total)
        {
            currentWave = wave;
            totalWaves = total;
        }

        private void HandlePreparation(int nextWave, int seconds)
        {
            preparing = true;
            currentWave = nextWave;
            preparationSeconds = seconds;
        }

        private void HandlePreparationEnded()
        {
            preparing = false;
        }

        private string GetWaveText()
        {
            if (preparing)
            {
                return
                    $"Fase {currentPhase}/{totalPhases} | " +
                    $"Onda {currentWave} em {preparationSeconds}s";
            }

            return
                $"Fase: {currentPhase}/{totalPhases} | " +
                $"Onda: {currentWave}/{totalWaves}";
        }

        private void HandleGameOver()
        {
            gameOver = true;
            Time.timeScale = 0f;
        }

        private void HandleKinDown()
        {
            kinDown = true;
            Time.timeScale = 0f;
        }

        private void HandleVictory()
        {
            victory = true;
            Time.timeScale = 0f;
        }

        private static GUIStyle CreateStyle(
            int size,
            Color color,
            FontStyle fontStyle)
        {
            return new GUIStyle
            {
                fontSize = size,
                fontStyle = fontStyle,
                normal = { textColor = color }
            };
        }
    }
}
