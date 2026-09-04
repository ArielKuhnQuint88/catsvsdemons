using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using CatsVsDemons.Player;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    public sealed class ResponsiveCanvasHud : MonoBehaviour
    {
        private RuntimeUiFactory ui;
        private Canvas rootCanvas;
        private RectTransform safeArea;
        private Rect lastSafeArea;
        private Wallet wallet;
        private HouseHealth house;
        private KinHealth kin;
        private KinEnergy energy;
        private KinSpecialAttack special;
        private EnemyWaveSpawner waves;
        private Image houseBar;
        private Image kinBar;
        private Image energyBar;
        private Text stats;
        private Text countdown;
        private Text specialLabel;
        private Text pauseLabel;
        private Text resultTitle;
        private Text resultMessage;
        private Image resultBackground;
        private Text tutorialTitle;
        private Text tutorialBody;
        private Text tutorialAction;
        private Button specialButton;
        private Button pauseButton;
        private readonly Button[] defenseButtons = new Button[3];
        private readonly DefenseType[] defenseTypes =
        {
            DefenseType.Portal, DefenseType.Bonsai, DefenseType.Lantern
        };
        private GameObject pausePanel;
        private GameObject resultPanel;
        private GameObject tutorialPanel;
        private Sprite resultBackgroundSprite;
        private Texture2D resultBackgroundTexture;
        private bool ownsResultBackgroundTexture;
        private int phase;
        private int totalPhases;
        private int wave;
        private int totalWaves;
        private int preparation;
        private bool preparing;
        private bool paused;
        private bool ended;

        private void Awake()
        {
            ui = new RuntimeUiFactory();
            rootCanvas = ui.CreateCanvas(transform);
            safeArea = ui.Rect("Safe Area", rootCanvas.transform);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            BuildTopPanel();
            BuildActions();
            BuildOverlays();
            ApplySafeArea();
        }

        private void Start()
        {
            FindSystems();
            Subscribe();
            if (GetComponent<TutorialDirector>() == null)
                gameObject.AddComponent<TutorialDirector>();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea) ApplySafeArea();
            if (kin == null || energy == null) FindSystems();
            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearResultArtwork();
            Time.timeScale = 1f;
        }

        public void ShowTutorial(string title, string body, string action)
        {
            tutorialPanel.SetActive(true);
            tutorialTitle.text = title;
            tutorialBody.text = body;
            tutorialAction.text = action;
        }

        public void HideTutorial() => tutorialPanel.SetActive(false);

        public void SetHudVisible(bool visible)
        {
            if (rootCanvas != null)
            {
                rootCanvas.gameObject.SetActive(visible);
            }
        }

        public void SetHousePauseState(bool isPaused)
        {
            paused = isPaused;
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (pauseButton != null && !ended)
            {
                pauseButton.gameObject.SetActive(!isPaused);
            }

            if (pauseLabel != null)
            {
                pauseLabel.text = "PAUSAR";
            }
        }

        private void FindSystems()
        {
            wallet ??= Object.FindFirstObjectByType<Wallet>();
            house ??= Object.FindFirstObjectByType<HouseHealth>();
            kin ??= Object.FindFirstObjectByType<KinHealth>();
            waves ??= Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if (kin != null)
            {
                energy ??= kin.GetComponent<KinEnergy>();
                special ??= kin.GetComponent<KinSpecialAttack>();
            }
        }

        private void Subscribe()
        {
            if (house != null) house.Destroyed += OnHouseDestroyed;
            if (kin != null) kin.Downed += OnKinDown;
            if (waves == null) return;
            phase = waves.CurrentPhase;
            totalPhases = waves.TotalPhases;
            wave = waves.CurrentWave;
            totalWaves = waves.TotalWaves;
            waves.PhaseStarted += OnPhase;
            waves.WaveStarted += OnWave;
            waves.PreparationChanged += OnPreparation;
            waves.PreparationEnded += OnPreparationEnded;
            waves.Victory += OnVictory;
        }

        private void Unsubscribe()
        {
            if (house != null) house.Destroyed -= OnHouseDestroyed;
            if (kin != null) kin.Downed -= OnKinDown;
            if (waves == null) return;
            waves.PhaseStarted -= OnPhase;
            waves.WaveStarted -= OnWave;
            waves.PreparationChanged -= OnPreparation;
            waves.PreparationEnded -= OnPreparationEnded;
            waves.Victory -= OnVictory;
        }

        private void BuildTopPanel()
        {
            // A dark lacquer frame gives the washi card a stronger silhouette
            // against bright maps without making the HUD heavier.
            RectTransform frame = ui.Panel("Status Frame", safeArea,
                new Color(0.15f, 0.055f, 0.025f, 0.96f),
                new Vector2(792, 322));
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
            frame.pivot = new Vector2(0.5f, 1f);
            frame.anchoredPosition = new Vector2(0, -14);
            frame.GetComponent<Image>().raycastTarget = false;

            RectTransform panel = ui.Panel("Status", safeArea,
                RuntimeUiFactory.Paper, new Vector2(760, 310));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0, -18);

            RectTransform accent = ui.Panel("Status Gold Accent", panel,
                RuntimeUiFactory.Gold, new Vector2(704, 5));
            accent.anchorMin = accent.anchorMax = new Vector2(0.5f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.anchoredPosition = new Vector2(0, -9);
            accent.GetComponent<Image>().raycastTarget = false;

            AddBarRow(panel, "CASA", -20, out houseBar,
                new Color(0.88f, 0.12f, 0.08f), 20);
            AddBarRow(panel, "KIN", -54, out kinBar,
                new Color(0.12f, 0.72f, 0.25f), 16);
            AddBarRow(panel, "ENERGIA", -86, out energyBar,
                RuntimeUiFactory.Gold, 13);
            stats = ui.Label("Stats", "", panel, 20, RuntimeUiFactory.Ink,
                new Vector2(720, 38), TextAnchor.MiddleCenter);
            RectTransform statsRect = (RectTransform)stats.transform;
            statsRect.anchorMin = statsRect.anchorMax = new Vector2(0.5f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.anchoredPosition = new Vector2(0, -116);

            string[] labels = { "PORTAL  10", "BONSAI  15", "LANTERNA  10" };
            string[] artwork = { "TowerPortal", "TowerBonsai", "TowerLantern" };
            Color[] colors =
            {
                new(0.04f, 0.38f, 0.78f), new(0.05f, 0.55f, 0.2f),
                new(0.55f, 0.24f, 0.72f)
            };
            for (int index = 0; index < defenseButtons.Length; index++)
            {
                AddDefenseButton(panel, index, labels[index], artwork[index],
                    colors[index]);
            }
        }

        private void AddDefenseButton(RectTransform panel, int index,
            string label, string artwork, Color color)
        {
            int captured = index;
            Button button = ui.Button($"Defense {index}", label, panel,
                new Color(color.r, color.g, color.b, 0.92f),
                new Vector2(206, 116));
            defenseButtons[index] = button;
            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = new Vector2((index - 1) * 226, -174);

            Text labelText = button.GetComponentInChildren<Text>();
            RectTransform labelRect = (RectTransform)labelText.transform;
            labelRect.sizeDelta = new Vector2(194, 30);
            labelRect.anchoredPosition = new Vector2(0, -39);
            labelText.fontSize = 19;
            labelText.color = RuntimeUiFactory.Ink;

            RectTransform border = ui.Rect("Golden Circle", button.transform);
            border.sizeDelta = new Vector2(82, 82);
            border.anchoredPosition = new Vector2(0, 15);
            Image borderImage = border.gameObject.AddComponent<Image>();
            borderImage.sprite = RuntimeUiFactory.CircleSprite;
            borderImage.color = RuntimeUiFactory.Gold;
            borderImage.raycastTarget = false;

            RectTransform maskRect = ui.Rect("Artwork Mask", border);
            maskRect.sizeDelta = new Vector2(72, 72);
            Image maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.sprite = RuntimeUiFactory.CircleSprite;
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
            Mask mask = maskRect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            Texture2D texture = LoadArtwork(artwork);
            if (texture != null)
            {
                RectTransform iconRect = ui.Rect("Artwork", maskRect);
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                icon.preserveAspect = false;
                icon.raycastTarget = false;
            }

            button.onClick.AddListener(() =>
                TowerBuildSelection.Select(defenseTypes[captured]));
        }

        private void AddBarRow(RectTransform panel, string name, float y,
            out Image bar, Color color, float height)
        {
            Text label = ui.Label(name, name, panel, 19, RuntimeUiFactory.Ink,
                new Vector2(96, 28), TextAnchor.MiddleLeft);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(22, y);
            bar = ui.Bar($"{name} Bar", panel, new Vector2(568, height));
            bar.color = color;
            RectTransform barRect = (RectTransform)bar.transform.parent;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(124, y - 4);
        }

        private void BuildActions()
        {
            // Keep the controls inside a thumb-friendly zone rather than flush
            // against the edge. Some Game View and phone safe-area combinations
            // crop strict right-edge anchors.
            Vector2 specialAnchor = new Vector2(0.84f, 0f);
            Vector2 specialPivot = new Vector2(0.5f, 0f);
            Vector2 specialPosition = new Vector2(0f, 34f);
            CreateCircleLayer("Special Shadow", safeArea, specialAnchor,
                specialPivot, specialPosition, new Vector2(202, 202),
                new Color(0.02f, 0.012f, 0.01f, 0.78f));
            CreateCircleLayer("Special Gold Rim", safeArea, specialAnchor,
                specialPivot, specialPosition, new Vector2(194, 194),
                RuntimeUiFactory.Gold);

            specialButton = ui.Button("Special", "GOLPE\n0%", safeArea,
                RuntimeUiFactory.Paper, new Vector2(174, 174));
            RectTransform specialRect = (RectTransform)specialButton.transform;
            Position(specialRect, specialAnchor, specialPivot, specialPosition);
            Image specialImage = specialButton.GetComponent<Image>();
            specialImage.sprite = RuntimeUiFactory.CircleSprite;
            specialImage.type = Image.Type.Simple;
            specialButton.targetGraphic = specialImage;
            ColorBlock specialColors = specialButton.colors;
            specialColors.highlightedColor = new Color(1f, 0.92f, 0.67f, 1f);
            specialColors.pressedColor = RuntimeUiFactory.Gold;
            specialColors.disabledColor = new Color(0.68f, 0.56f, 0.34f, 0.95f);
            specialButton.colors = specialColors;
            specialLabel = specialButton.GetComponentInChildren<Text>();
            specialLabel.color = RuntimeUiFactory.Ink;
            specialLabel.fontSize = 22;
            specialLabel.lineSpacing = 0.82f;
            specialButton.onClick.AddListener(() => special?.TryUse());

            Vector2 pauseAnchor = new Vector2(0.89f, 1f);
            Vector2 pausePivot = new Vector2(0.5f, 1f);
            Vector2 pausePosition = new Vector2(0f, -24f);
            RectTransform pauseShadow = ui.Panel("Pause Shadow", safeArea,
                RuntimeUiFactory.Ink, new Vector2(164, 68));
            Position(pauseShadow, pauseAnchor, pausePivot, pausePosition);
            pauseShadow.GetComponent<Image>().raycastTarget = false;
            RectTransform pauseRim = ui.Panel("Pause Gold Rim", safeArea,
                RuntimeUiFactory.Gold, new Vector2(156, 60));
            Position(pauseRim, pauseAnchor, pausePivot, pausePosition);
            pauseRim.GetComponent<Image>().raycastTarget = false;

            pauseButton = ui.Button("Pause", "PAUSAR", safeArea,
                RuntimeUiFactory.Paper, new Vector2(146, 50));
            RectTransform pauseRect = (RectTransform)pauseButton.transform;
            Position(pauseRect, pauseAnchor, pausePivot, pausePosition);
            pauseLabel = pauseButton.GetComponentInChildren<Text>();
            pauseLabel.color = RuntimeUiFactory.Ink;
            pauseLabel.fontSize = 19;
            pauseButton.onClick.AddListener(TogglePause);
        }

        private Image CreateCircleLayer(string name, Transform parent,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            Color color)
        {
            RectTransform rect = ui.Rect(name, parent);
            rect.sizeDelta = size;
            Position(rect, anchor, pivot, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = RuntimeUiFactory.CircleSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Position(RectTransform rect, Vector2 anchor,
            Vector2 pivot, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
        }

        private void BuildOverlays()
        {
            countdown = ui.Label("Countdown", "", safeArea, 76,
                RuntimeUiFactory.Gold, new Vector2(720, 200),
                TextAnchor.MiddleCenter);
            countdown.gameObject.SetActive(false);

            pausePanel = FullScreenPanel("Pause Panel");
            Text pausedTitle = ui.Label("Pause Title", "PAUSADO",
                pausePanel.transform, 58, Color.white, new Vector2(700, 100),
                TextAnchor.MiddleCenter);
            ((RectTransform)pausedTitle.transform).anchoredPosition =
                new Vector2(0, 170);
            Button resume = ui.Button("Resume", "CONTINUAR", pausePanel.transform,
                RuntimeUiFactory.Gold, new Vector2(320, 74));
            ((RectTransform)resume.transform).anchoredPosition = new Vector2(0, 38);
            resume.onClick.AddListener(TogglePause);
            Button restart = ui.Button("Restart", "REINICIAR", pausePanel.transform,
                new Color(0.58f, 0.07f, 0.05f), new Vector2(320, 74));
            ((RectTransform)restart.transform).anchoredPosition = new Vector2(0, -58);
            restart.onClick.AddListener(Restart);
            pausePanel.SetActive(false);

            resultPanel = FullScreenPanel("Result Panel");
            resultBackground = resultPanel.GetComponent<Image>();
            resultBackground.type = Image.Type.Simple;
            resultBackground.preserveAspect = false;

            RectTransform shadeRect = ui.Rect("Result Shade",
                resultPanel.transform);
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            Image shade = shadeRect.gameObject.AddComponent<Image>();
            shade.color = new Color(0.01f, 0.012f, 0.025f, 0.38f);
            shade.raycastTarget = false;

            resultTitle = ui.Label("Result Title", "", resultPanel.transform, 58,
                Color.white, new Vector2(1300, 100), TextAnchor.MiddleCenter);
            ((RectTransform)resultTitle.transform).anchoredPosition =
                new Vector2(0, 210);
            resultMessage = ui.Label("Result Message", "", resultPanel.transform,
                26, Color.white, new Vector2(1300, 80), TextAnchor.MiddleCenter);
            ((RectTransform)resultMessage.transform).anchoredPosition =
                new Vector2(0, 80);
            Button resultRestart = ui.Button("Result Restart", "REINICIAR",
                resultPanel.transform, RuntimeUiFactory.Gold,
                new Vector2(320, 76));
            ((RectTransform)resultRestart.transform).anchoredPosition =
                new Vector2(0, -120);
            resultRestart.onClick.AddListener(Restart);
            resultPanel.SetActive(false);

            RectTransform tutorial = ui.Panel("Tutorial", safeArea,
                new Color(0.03f, 0.018f, 0.045f, 0.96f),
                new Vector2(820, 178));
            tutorial.anchorMin = tutorial.anchorMax = new Vector2(0.5f, 0);
            tutorial.pivot = new Vector2(0.5f, 0);
            tutorial.anchoredPosition = new Vector2(-210, 190);
            tutorialPanel = tutorial.gameObject;
            tutorialTitle = ui.Label("Tutorial Title", "", tutorial, 25,
                RuntimeUiFactory.Gold, new Vector2(776, 36),
                TextAnchor.MiddleLeft);
            ((RectTransform)tutorialTitle.transform).anchoredPosition =
                new Vector2(0, 56);
            tutorialBody = ui.Label("Tutorial Body", "", tutorial, 20,
                Color.white, new Vector2(776, 62), TextAnchor.UpperLeft);
            ((RectTransform)tutorialBody.transform).anchoredPosition =
                new Vector2(0, 5);
            tutorialAction = ui.Label("Tutorial Action", "", tutorial, 18,
                new Color(0.55f, 0.9f, 1f), new Vector2(776, 34),
                TextAnchor.MiddleLeft);
            ((RectTransform)tutorialAction.transform).anchoredPosition =
                new Vector2(0, -58);
            tutorialPanel.SetActive(false);
        }

        private GameObject FullScreenPanel(string name)
        {
            RectTransform panel = ui.Panel(name, safeArea,
                new Color(0.01f, 0.012f, 0.025f, 0.94f), Vector2.zero);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            return panel.gameObject;
        }

        private void Refresh()
        {
            SetFill(houseBar, house != null ? house.CurrentHealth : 0,
                house != null ? house.MaxHealth : 1);
            SetFill(kinBar, kin != null ? kin.CurrentHealth : 0,
                kin != null ? kin.MaxHealth : 1);
            SetFill(energyBar, energy != null ? energy.Current : 0,
                energy != null ? energy.Maximum : 100);
            stats.text = $"MOEDAS  {(wallet != null ? wallet.Coins : 0)}" +
                $"     FASE  {phase}/{totalPhases}     ONDA  {wave}/{totalWaves}" +
                $"     DEMÔNIOS  {EnemyRegistry.Count}";
            int energyPercent = energy != null
                ? Mathf.RoundToInt(energy.Normalized * 100f) : 0;
            specialLabel.text = energy != null && energy.IsFull
                ? "GOLPE\nPRONTO!" : $"GOLPE\n{energyPercent}%";
            specialButton.interactable = energy != null && energy.IsFull && !ended;
            countdown.gameObject.SetActive(preparing && !paused && !ended);
            if (preparing) countdown.text = $"FASE {phase}\n{preparation}";

            for (int index = 0; index < 3; index++)
            {
                Image image = defenseButtons[index].GetComponent<Image>();
                image.transform.localScale =
                    TowerBuildSelection.Selected == defenseTypes[index]
                    ? Vector3.one * 1.08f : Vector3.one;
            }
        }

        private static Texture2D LoadArtwork(string name)
        {
            TextAsset encoded = Resources.Load<TextAsset>($"UI/{name}Data");
            if (encoded != null)
            {
                Texture2D decoded = new(2, 2, TextureFormat.RGBA32, false)
                {
                    name = $"{name}_Hud"
                };
                if (ImageConversion.LoadImage(decoded, encoded.bytes, true))
                    return decoded;
                Object.Destroy(decoded);
            }
            return Resources.Load<Texture2D>($"UI/{name}");
        }

        private void TogglePause()
        {
            if (ended) return;

            if (paused)
            {
                paused = false;
                Time.timeScale = 1f;
                pausePanel.SetActive(false);
                pauseLabel.text = "PAUSAR";
                return;
            }

            HouseIntermissionController house =
                Object.FindFirstObjectByType<HouseIntermissionController>();
            if (house != null && house.OpenPauseRoom())
            {
                return;
            }

            paused = true;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            pauseLabel.text = "CONTINUAR";
        }

        private void ShowResult(string title, string message,
            string artworkName)
        {
            if (ended) return;
            ended = true;
            Time.timeScale = 0f;
            SetResultArtwork(artworkName);
            resultTitle.text = title;
            resultMessage.text = message;
            resultPanel.transform.SetAsLastSibling();
            resultPanel.SetActive(true);
            pauseButton.gameObject.SetActive(false);
            HideTutorial();
            Canvas.ForceUpdateCanvases();
        }

        private void SetResultArtwork(string artworkName)
        {
            ClearResultArtwork();
            Texture2D artwork = LoadArtwork(artworkName);
            if (artwork == null)
            {
                resultBackground.sprite = null;
                resultBackground.color =
                    new Color(0.01f, 0.012f, 0.025f, 0.94f);
                Debug.LogError($"[CatsVsDemons] Arte final não encontrada: " +
                    $"Resources/UI/{artworkName}Data.bytes ou {artworkName}.jpg");
                return;
            }

            resultBackgroundTexture = artwork;
            ownsResultBackgroundTexture = artwork.name == $"{artworkName}_Hud";
            resultBackgroundSprite = Sprite.Create(artwork,
                new Rect(0f, 0f, artwork.width, artwork.height),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            resultBackgroundSprite.name = $"{artworkName}_Result";
            resultBackground.sprite = resultBackgroundSprite;
            resultBackground.type = Image.Type.Simple;
            resultBackground.preserveAspect = false;
            resultBackground.color = Color.white;
            resultBackground.enabled = true;
        }

        private void ClearResultArtwork()
        {
            if (resultBackgroundSprite != null)
                Destroy(resultBackgroundSprite);
            if (ownsResultBackgroundTexture && resultBackgroundTexture != null)
                Destroy(resultBackgroundTexture);
            resultBackgroundSprite = null;
            resultBackgroundTexture = null;
            ownsResultBackgroundTexture = false;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            safeArea.anchorMin = min;
            safeArea.anchorMax = max;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        private static void SetFill(Image image, float current, float maximum)
        {
            image.fillAmount = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        }

        private void OnPhase(int value, int total)
        {
            phase = value;
            totalPhases = total;
            wave = 0;
        }
        private void OnWave(int value, int total)
        {
            wave = value;
            totalWaves = total;
        }
        private void OnPreparation(int value, int seconds)
        {
            preparing = true;
            wave = value;
            preparation = seconds;
        }
        private void OnPreparationEnded() => preparing = false;
        private void OnHouseDestroyed() => ShowResult("A CASA CAIU!",
            "Mesmo ferido, Kin fez tudo o que pôde.", "EndingDefeat");
        private void OnKinDown() => ShowResult("KIN FOI DERROTADO!",
            "Seu dono nunca saberá como Kin tentou protegê-lo.",
            "EndingDefeat");
        private void OnVictory() => ShowResult("A CASA ESTÁ SEGURA!",
            "O guardião da noite venceu mais uma batalha.", "EndingVictory");
    }
}
