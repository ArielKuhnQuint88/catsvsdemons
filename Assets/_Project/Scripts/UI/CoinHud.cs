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

            bool mobile = ResponsiveGuiTheme.IsMobile;
            float scale = mobile
                ? Mathf.Clamp(Screen.height / 720f, 1f, 1.5f)
                : Mathf.Clamp(Screen.height / 900f, 0.8f, 1f);
            float width = mobile
                ? Mathf.Min(Screen.width * 0.44f, 660f * scale)
                : Mathf.Clamp(Screen.width * 0.46f, 430f, 560f);
            float height = 235f * scale;
            Rect frame = new Rect(
                (Screen.width - width) * 0.5f,
                12f * scale,
                width,
                height
            );

            // Sombra, moldura de madeira e filete dourado.
            DrawTexture(
                new Rect(
                    frame.x + 6f * scale,
                    frame.y + 7f * scale,
                    frame.width,
                    frame.height
                ),
                new Color(0f, 0f, 0f, 0.42f)
            );
            DrawTexture(frame, new Color(0.12f, 0.025f, 0.018f, 0.98f));
            Rect goldFrame = new Rect(
                frame.x + 5f * scale,
                frame.y + 5f * scale,
                frame.width - 10f * scale,
                frame.height - 10f * scale
            );
            DrawTexture(goldFrame, new Color(0.72f, 0.38f, 0.08f, 1f));
            Rect paper = new Rect(
                goldFrame.x + 3f * scale,
                goldFrame.y + 3f * scale,
                goldFrame.width - 6f * scale,
                goldFrame.height - 6f * scale
            );
            DrawPaper(paper);

            // Cabeçalho de vida em duas linhas bem separadas.
            float pad = 16f * scale;
            float labelWidth = 82f * scale;
            float barLeft = paper.x + pad + labelWidth;
            float barWidth = paper.xMax - pad - barLeft;
            compactStyle.fontSize = Mathf.RoundToInt(15f * scale);
            compactCenterStyle.fontSize = Mathf.RoundToInt(13f * scale);

            GUI.Label(
                new Rect(paper.x + pad, paper.y + 12f * scale,
                    labelWidth, 24f * scale),
                "♥  CASA",
                compactStyle
            );
            DrawBar(
                new Rect(barLeft, paper.y + 14f * scale,
                    barWidth, 18f * scale),
                houseMax > 0 ? (float)houseHealth / houseMax : 0f,
                new Color(0.9f, 0.16f, 0.1f),
                $"{houseHealth}/{houseMax}"
            );

            GUI.Label(
                new Rect(paper.x + pad, paper.y + 40f * scale,
                    labelWidth, 24f * scale),
                "KIN",
                compactStyle
            );
            DrawBar(
                new Rect(barLeft, paper.y + 43f * scale,
                    barWidth, 13f * scale),
                kinMax > 0 ? (float)kinHealth / kinMax : 0f,
                HealthColor(kinHealth, kinMax),
                string.Empty
            );

            // Faixa central: cada informação ocupa uma célula própria.
            float statsY = paper.y + 70f * scale;
            float statsHeight = 38f * scale;
            Rect stats = new Rect(
                paper.x + 10f * scale,
                statsY,
                paper.width - 20f * scale,
                statsHeight
            );
            DrawTexture(stats, new Color(0.22f, 0.075f, 0.035f, 0.13f));
            float cellWidth = stats.width / 4f;
            DrawStatusCell(
                new Rect(stats.x, stats.y, cellWidth, stats.height),
                $"●  {coins}",
                new Color(0.68f, 0.32f, 0.04f)
            );
            DrawStatusCell(
                new Rect(stats.x + cellWidth, stats.y, cellWidth, stats.height),
                $"FASE  {currentPhase}/{totalPhases}",
                Color.black
            );
            DrawStatusCell(
                new Rect(stats.x + cellWidth * 2f, stats.y, cellWidth, stats.height),
                $"ONDA  {currentWave}/{totalWaves}",
                Color.black
            );
            DrawStatusCell(
                new Rect(stats.x + cellWidth * 3f, stats.y, cellWidth, stats.height),
                $"☠  {activeEnemies}",
                new Color(0.32f, 0.08f, 0.38f)
            );
            for (int index = 1; index < 4; index++)
            {
                DrawTexture(
                    new Rect(
                        stats.x + cellWidth * index,
                        stats.y + 7f * scale,
                        1f,
                        stats.height - 14f * scale
                    ),
                    new Color(0.33f, 0.13f, 0.04f, 0.32f)
                );
            }

            // Três colunas independentes para os botões das defesas.
            float buttonY = paper.y + 151f * scale;
            float radius = 32f * scale;
            Vector2[] centers =
            {
                new Vector2(paper.x + paper.width * 0.25f, buttonY),
                new Vector2(paper.x + paper.width * 0.50f, buttonY),
                new Vector2(paper.x + paper.width * 0.75f, buttonY)
            };
            DrawDefenseButton(
                centers[0], radius, "PORTAL", 10, DefenseType.Portal,
                new Color(0.03f, 0.46f, 0.92f), portalIcon
            );
            DrawDefenseButton(
                centers[1], radius, "BONSAI", 15, DefenseType.Bonsai,
                new Color(0.08f, 0.62f, 0.22f), bonsaiIcon
            );
            DrawDefenseButton(
                centers[2], radius, "LANTERNA", 10, DefenseType.Lantern,
                new Color(0.56f, 0.30f, 0.76f), lanternIcon
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
            bool hovered =
                Vector2.Distance(Event.current.mousePosition, center) <= radius;
            float outerRadius = selected ? radius + 6f : radius + 3f;

            if (selected)
                DrawCircle(center, radius + 10f, new Color(1f, 0.66f, 0.12f, 0.22f));

            DrawCircle(
                center,
                outerRadius,
                selected
                    ? new Color(0.96f, 0.62f, 0.12f, 1f)
                    : new Color(0.12f, 0.045f, 0.025f, 0.96f)
            );
            DrawCircle(
                center,
                radius,
                hovered ? Color.Lerp(color, Color.white, 0.16f) : color
            );
            DrawCircle(
                center,
                radius * 0.84f,
                new Color(0.018f, 0.04f, 0.06f, 0.82f)
            );

            if (icon != null)
            {
                float iconSize = radius *
                    (type == DefenseType.Lantern ? 1.92f : 1.76f);
                GUI.DrawTexture(
                    new Rect(
                        center.x - iconSize * 0.5f,
                        center.y - iconSize * 0.5f,
                        iconSize,
                        iconSize
                    ),
                    icon,
                    ScaleMode.ScaleToFit,
                    true
                );
            }

            radialStyle.fontSize = Mathf.RoundToInt(
                Mathf.Clamp(radius * 0.35f, 10f, 15f)
            );
            costStyle.fontSize = Mathf.RoundToInt(
                Mathf.Clamp(radius * 0.34f, 10f, 15f)
            );
            radialStyle.normal.textColor = selected
                ? new Color(0.38f, 0.08f, 0.025f)
                : Color.black;
            costStyle.normal.textColor = new Color(0.31f, 0.13f, 0.025f);

            float labelY = center.y + radius + 5f;
            GUI.Label(
                new Rect(center.x - radius * 1.35f, labelY,
                    radius * 2.7f, 19f * ResponsiveGuiTheme.LayoutScale),
                label,
                radialStyle
            );
            GUI.Label(
                new Rect(center.x - radius * 1.35f,
                    labelY + 18f * ResponsiveGuiTheme.LayoutScale,
                    radius * 2.7f,
                    19f * ResponsiveGuiTheme.LayoutScale),
                $"●  {cost}",
                costStyle
            );

            if (hovered &&
                Event.current.type == EventType.MouseUp &&
                Event.current.button == 0)
            {
                TowerBuildSelection.Select(type);
                Event.current.Use();
            }
        }

        private void DrawStatusCell(Rect area, string text, Color color)
        {
            Color previous = compactCenterStyle.normal.textColor;
            compactCenterStyle.normal.textColor = color;
            compactCenterStyle.alignment = TextAnchor.MiddleCenter;
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
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            )
            {
                name = "HudWashiTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.055f, y * 0.055f);
                    float fiber =
                        Mathf.Sin(y * 0.24f + x * 0.018f) * 0.009f;
                    float shade = Mathf.Lerp(-0.028f, 0.026f, grain) + fiber;
                    pixels[y * size + x] = new Color(
                        0.91f + shade,
                        0.84f + shade,
                        0.68f + shade * 0.72f,
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
                GUI.DrawTexture(screen, background, ScaleMode.ScaleAndCrop);
            else
                GUI.Box(screen, GUIContent.none);

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

            float scale = ResponsiveGuiTheme.LayoutScale;
            float width = (ResponsiveGuiTheme.IsMobile ? 260f : 190f) * scale;
            float height = (ResponsiveGuiTheme.IsMobile ? 62f : 46f) * scale;
            if (ResponsiveGuiTheme.Button(
                new Rect(
                    (Screen.width - width) * 0.5f,
                    Screen.height - height - 22f * scale,
                    width,
                    height
                ),
                "REINICIAR",
                ResponsiveGuiTheme.ButtonTone.Gold,
                Mathf.RoundToInt(18f * scale)))
            {
                RestartGame();
            }
        }

        private void DrawPauseButton()
        {
            float scale = ResponsiveGuiTheme.LayoutScale;
            float width = (ResponsiveGuiTheme.IsMobile ? 140f : 96f) * scale;
            float height = (ResponsiveGuiTheme.IsMobile ? 52f : 36f) * scale;
            float margin = 18f * scale;
            if (ResponsiveGuiTheme.Button(
                new Rect(Screen.width - width - margin, margin, width, height),
                paused ? "CONTINUAR" : "PAUSAR",
                ResponsiveGuiTheme.ButtonTone.Ink,
                Mathf.RoundToInt(14f * scale)))
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void DrawPauseMenu()
        {
            DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.01f, 0.02f, 0.04f, 0.9f)
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.30f, Screen.width, 90f),
                "PAUSADO",
                resultStyle
            );

            float scale = ResponsiveGuiTheme.LayoutScale;
            float width = (ResponsiveGuiTheme.IsMobile ? 270f : 190f) * scale;
            float height = (ResponsiveGuiTheme.IsMobile ? 64f : 46f) * scale;
            float gap = 16f * scale;
            float left = (Screen.width - width) * 0.5f;
            float top = Screen.height * 0.48f;
            int fontSize = Mathf.RoundToInt(18f * scale);

            if (ResponsiveGuiTheme.Button(
                new Rect(left, top, width, height),
                "CONTINUAR",
                ResponsiveGuiTheme.ButtonTone.Gold,
                fontSize))
            {
                paused = false;
                Time.timeScale = 1f;
            }

            if (ResponsiveGuiTheme.Button(
                new Rect(left, top + height + gap, width, height),
                "REINICIAR",
                ResponsiveGuiTheme.ButtonTone.Crimson,
                fontSize))
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

    internal static class ResponsiveGuiTheme
    {
        internal enum ButtonTone
        {
            Crimson,
            Azure,
            Gold,
            Ink
        }

        private static GUIStyle crimson;
        private static GUIStyle azure;
        private static GUIStyle gold;
        private static GUIStyle ink;

        public static bool IsMobile =>
            Application.isMobilePlatform ||
            SystemInfo.deviceType == DeviceType.Handheld;

        public static float LayoutScale => IsMobile
            ? Mathf.Clamp(Screen.height / 720f, 1f, 2f)
            : Mathf.Clamp(Screen.height / 1080f, 0.72f, 1f);

        public static bool Button(
            Rect area,
            string label,
            ButtonTone tone,
            int fontSize)
        {
            EnsureStyles();
            GUIStyle style = tone switch
            {
                ButtonTone.Crimson => crimson,
                ButtonTone.Azure => azure,
                ButtonTone.Gold => gold,
                _ => ink
            };
            style.fontSize = Mathf.Max(12, fontSize);

            Rect shadow = new Rect(
                area.x + 3f * LayoutScale,
                area.y + 5f * LayoutScale,
                area.width,
                area.height
            );
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.38f);
            GUI.DrawTexture(shadow, style.normal.background, ScaleMode.StretchToFill);
            GUI.color = previous;
            return GUI.Button(area, label, style);
        }

        private static void EnsureStyles()
        {
            if (crimson != null)
                return;

            crimson = CreateStyle(
                new Color(0.48f, 0.055f, 0.045f),
                new Color(0.19f, 0.018f, 0.025f),
                new Color(0.94f, 0.55f, 0.16f),
                Color.white
            );
            azure = CreateStyle(
                new Color(0.055f, 0.30f, 0.52f),
                new Color(0.018f, 0.08f, 0.18f),
                new Color(0.16f, 0.72f, 0.96f),
                Color.white
            );
            gold = CreateStyle(
                new Color(1f, 0.78f, 0.24f),
                new Color(0.68f, 0.32f, 0.055f),
                new Color(1f, 0.92f, 0.52f),
                new Color(0.12f, 0.055f, 0.018f)
            );
            ink = CreateStyle(
                new Color(0.16f, 0.13f, 0.22f),
                new Color(0.035f, 0.025f, 0.07f),
                new Color(0.62f, 0.46f, 0.82f),
                Color.white
            );
        }

        private static GUIStyle CreateStyle(
            Color top,
            Color bottom,
            Color border,
            Color text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                wordWrap = false,
                border = new RectOffset(18, 18, 16, 16),
                padding = new RectOffset(14, 14, 6, 6)
            };
            style.normal.background = CreateRoundedTexture(top, bottom, border);
            style.hover.background = CreateRoundedTexture(
                Color.Lerp(top, Color.white, 0.16f),
                Color.Lerp(bottom, Color.white, 0.08f),
                Color.Lerp(border, Color.white, 0.28f)
            );
            style.active.background = CreateRoundedTexture(
                Color.Lerp(top, Color.black, 0.2f),
                Color.Lerp(bottom, Color.black, 0.28f),
                border
            );
            style.normal.textColor = text;
            style.hover.textColor = Color.white;
            style.active.textColor = text;
            return style;
        }

        private static Texture2D CreateRoundedTexture(
            Color top,
            Color bottom,
            Color border)
        {
            const int width = 128;
            const int height = 56;
            const float radius = 14f;
            const float borderWidth = 3f;
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            )
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float blend = y / (height - 1f);
                Color fill = Color.Lerp(bottom, top, blend);
                for (int x = 0; x < width; x++)
                {
                    bool outer = InsideRounded(x, y, width, height, radius);
                    bool inner = InsideRounded(
                        x - borderWidth,
                        y - borderWidth,
                        width - borderWidth * 2f,
                        height - borderWidth * 2f,
                        radius - borderWidth
                    );
                    Color pixel = !outer
                        ? Color.clear
                        : !inner
                            ? border
                            : fill;
                    pixels[y * width + x] = pixel;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool InsideRounded(
            float x,
            float y,
            float width,
            float height,
            float radius)
        {
            if (x < 0f || y < 0f || x >= width || y >= height)
                return false;

            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float dx = Mathf.Max(
                Mathf.Abs(x - centerX) - (width * 0.5f - radius),
                0f
            );
            float dy = Mathf.Max(
                Mathf.Abs(y - centerY) - (height * 0.5f - radius),
                0f
            );
            return dx * dx + dy * dy <= radius * radius;
        }
    }

}
