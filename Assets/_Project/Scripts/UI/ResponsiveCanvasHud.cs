using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using CatsVsDemons.Player;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private Text coinsLabel;
        private Text coinDelta;
        private Text phaseBanner;
        private CanvasGroup phaseBannerGroup;
        private HudBarAnimator houseBarAnimation;
        private HudBarAnimator kinBarAnimation;
        private HudBarAnimator energyBarAnimation;
        private HudMotion coinMotion;
        private HudMotion countdownMotion;
        private HudMotion specialMotion;
        private Image energyRing;
        private float coinDeltaTime;
        private float bannerTime;
        private float nextSystemSearch;
        private int displayedCoins = int.MinValue;
        private int displayedPhase = -1;
        private int displayedWave = -1;
        private int displayedEnemies = -1;
        private int displayedEnergy = -1;
        private int displayedPreparation = -1;
        private bool displayedReady;
        private Text countdown;
        private Text specialLabel;
        private Text pauseLabel;
        private Text resultTitle;
        private Text resultMessage;
        private Image resultBackground;
        private KinTutorialPresenter tutorial;
        private Button specialButton;
        private Button pauseButton;
        private readonly Button[] defenseButtons = new Button[3];
        private readonly HudMotion[] defenseMotions = new HudMotion[3];
        private readonly DefenseType[] defenseTypes =
        {
            DefenseType.Portal, DefenseType.Bonsai, DefenseType.Lantern
        };
        private GameObject pausePanel;
        private GameObject resultPanel;
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
        private readonly System.Collections.Generic.List<Object> ownedArtwork = new();

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
            houseBarAnimation = houseBar.gameObject.AddComponent<HudBarAnimator>();
            houseBarAnimation.Initialize(houseBar, true);
            kinBarAnimation = kinBar.gameObject.AddComponent<HudBarAnimator>();
            kinBarAnimation.Initialize(kinBar, true);
            energyBarAnimation = energyBar.gameObject.AddComponent<HudBarAnimator>();
            energyBarAnimation.Initialize(energyBar, false);
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
            if ((kin == null || energy == null || house == null ||
                wallet == null || waves == null) && Time.unscaledTime >= nextSystemSearch)
            {
                nextSystemSearch = Time.unscaledTime + 0.5f;
                Unsubscribe();
                FindSystems();
                Subscribe();
            }
            if (rootCanvas.gameObject.activeInHierarchy && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePause();
            Refresh();
            AnimateAnnouncements();
            tutorial.SetSuspended(!CanGuideTutorial);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearResultArtwork();
            foreach (Object artwork in ownedArtwork)
                if (artwork != null) Destroy(artwork);
            Time.timeScale = 1f;
        }

        public void ShowTutorial(string title, string body, string action)
        {
            tutorial.Show(title, body, action, () => GetComponent<TutorialDirector>()?.Skip());
        }

        public void HideTutorial() => tutorial.Hide();

        public void SetTutorialFocus(TutorialFocus kind, Transform target, string caption)
        {
            RectTransform control = kind == TutorialFocus.LanternButton
                ? (RectTransform)defenseButtons[2].transform
                : kind == TutorialFocus.SpecialButton ? (RectTransform)specialButton.transform : null;
            tutorial.SetFocus(kind, control, target, caption);
        }

        public void SetTutorialProgress(string text) => tutorial.SetProgress(text);
        public bool CanGuideTutorial => !paused && !ended && rootCanvas.gameObject.activeInHierarchy;

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
            if (wallet == null) wallet = Object.FindFirstObjectByType<Wallet>();
            if (house == null) house = Object.FindFirstObjectByType<HouseHealth>();
            if (kin == null) kin = Object.FindFirstObjectByType<KinHealth>();
            if (waves == null) waves = Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if (kin != null)
            {
                if (energy == null) energy = kin.GetComponent<KinEnergy>();
                if (special == null) special = kin.GetComponent<KinSpecialAttack>();
            }
        }

        private void Subscribe()
        {
            if (house != null) house.Destroyed += OnHouseDestroyed;
            if (kin != null) kin.Downed += OnKinDown;
            if (waves == null) return;
            phase = Mathf.Max(waves.StartingPhase, waves.CurrentPhase);
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
            RectTransform frame = ui.Panel("Status Frame", safeArea,
                new Color(0.12f, 0.22f, 0.21f, 0.93f), new Vector2(550, 188));
            Position(frame, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -18));
            frame.GetComponent<Image>().raycastTarget = false;
            RectTransform panel = ui.Panel("Status", frame, RuntimeUiFactory.Paper, new Vector2(540, 178));
            panel.GetComponent<Image>().raycastTarget = false;
            RectTransform accent = ui.Panel("Status Gold Accent", panel, RuntimeUiFactory.Gold, new Vector2(496, 3));
            Position(accent, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12));
            accent.GetComponent<Image>().raycastTarget = false;
            AddBarRow(panel, "CASA", -23, out houseBar, new Color(0.76f, 0.29f, 0.24f), 14);
            AddBarRow(panel, "KIN", -50, out kinBar, new Color(0.33f, 0.61f, 0.47f), 14);
            AddBarRow(panel, "ENERGIA", -77, out energyBar, RuntimeUiFactory.Gold, 10);
            coinsLabel = ui.Label("Coins", "MOEDAS  0", panel, 20, RuntimeUiFactory.Ink,
                new Vector2(170, 30), TextAnchor.MiddleLeft);
            RectTransform coinsRect = (RectTransform)coinsLabel.transform;
            Position(coinsRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -106));
            coinMotion = coinsRect.gameObject.AddComponent<HudMotion>();
            coinDelta = ui.Label("Coin Reward", "", coinsRect, 18, new Color(0.3f, 0.5f, 0.37f),
                new Vector2(170, 26), TextAnchor.MiddleLeft);
            coinDelta.gameObject.SetActive(false);
            stats = ui.Label("Stats", "", panel, 19, RuntimeUiFactory.Ink,
                new Vector2(496, 27), TextAnchor.MiddleLeft);
            Position((RectTransform)stats.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -143));

            RectTransform dock = ui.Panel("Defense Dock", safeArea, new Color(0.12f, 0.22f, 0.21f, 0.94f),
                new Vector2(472, 134));
            Position(dock, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24));
            dock.GetComponent<Image>().raycastTarget = false;
            string[] labels = { "PORTAL  10", "BONSAI  15", "LANTERNA  10" };
            string[] artwork = { "TowerPortal", "TowerBonsai", "TowerLantern" };
            Color[] colors = { new(0.82f, 0.87f, 0.89f), new(0.82f, 0.88f, 0.8f), new(0.87f, 0.83f, 0.89f) };
            for (int index = 0; index < defenseButtons.Length; index++)
                AddDefenseButton(dock, index, labels[index], artwork[index], colors[index]);
        }

        private void AddDefenseButton(RectTransform panel, int index,
            string label, string artwork, Color color)
        {
            int captured = index;
            Button button = ui.Button($"Defense {index}", label, panel,
                new Color(color.r, color.g, color.b, 0.92f),
                new Vector2(144, 116));
            defenseButtons[index] = button;
            defenseMotions[index] = button.GetComponent<HudMotion>();
            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = new Vector2((index - 1) * 152, -9);

            Text labelText = button.GetComponentInChildren<Text>();
            RectTransform labelRect = (RectTransform)labelText.transform;
            labelRect.sizeDelta = new Vector2(136, 28);
            labelRect.anchoredPosition = new Vector2(0, -39);
            labelText.fontSize = 20;
            labelText.resizeTextMaxSize = 20;
            labelText.color = RuntimeUiFactory.Ink;

            RectTransform border = ui.Rect("Golden Circle", button.transform);
            border.sizeDelta = new Vector2(74, 74);
            border.anchoredPosition = new Vector2(0, 15);
            Image borderImage = border.gameObject.AddComponent<Image>();
            borderImage.sprite = RuntimeUiFactory.CircleSprite;
            borderImage.color = RuntimeUiFactory.Gold;
            borderImage.raycastTarget = false;

            RectTransform maskRect = ui.Rect("Artwork Mask", border);
            maskRect.sizeDelta = new Vector2(66, 66);
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
                ownedArtwork.Add(icon.sprite);
                if (texture.name == $"{artwork}_Hud") ownedArtwork.Add(texture);
            }

            button.onClick.AddListener(() =>
                TowerBuildSelection.Select(defenseTypes[captured]));
        }

        private void AddBarRow(RectTransform panel, string name, float y,
            out Image bar, Color color, float height)
        {
            Text label = ui.Label(name, name, panel, 19, RuntimeUiFactory.Ink,
                new Vector2(90, 25), TextAnchor.MiddleLeft);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(22, y);
            bar = ui.Bar($"{name} Bar", panel, new Vector2(384, height));
            bar.color = color;
            RectTransform barRect = (RectTransform)bar.transform.parent;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(126, y - 4);
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
                specialPivot, specialPosition, new Vector2(174, 174),
                new Color(0.02f, 0.012f, 0.01f, 0.78f));
            CreateCircleLayer("Special Gold Rim", safeArea, specialAnchor,
                specialPivot, specialPosition, new Vector2(166, 166),
                new Color(0.24f, 0.14f, 0.06f));
            energyRing = CreateCircleLayer("Energy Charge Ring", safeArea,
                specialAnchor, specialPivot, specialPosition, new Vector2(170, 170),
                RuntimeUiFactory.Gold);
            energyRing.sprite = RuntimeUiFactory.RingSprite;
            energyRing.type = Image.Type.Filled;
            energyRing.fillMethod = Image.FillMethod.Radial360;
            energyRing.fillOrigin = (int)Image.Origin360.Top;
            energyRing.fillClockwise = true;
            energyRing.fillAmount = 0f;

            specialButton = ui.Button("Special", "GOLPE\n0%", safeArea,
                RuntimeUiFactory.Paper, new Vector2(150, 150));
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
            specialMotion = specialButton.GetComponent<HudMotion>();
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
            countdown.raycastTarget = false;
            countdownMotion = countdown.gameObject.AddComponent<HudMotion>();
            countdown.gameObject.AddComponent<Shadow>().effectColor =
                new Color(0.06f, 0.025f, 0.015f, 0.95f);

            RectTransform banner = ui.Panel("Phase Banner", safeArea,
                new Color(0.12f, 0.22f, 0.21f, 0.93f), new Vector2(760, 52));
            Position(banner, new Vector2(0.63f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24));
            banner.GetComponent<Image>().raycastTarget = false;
            phaseBannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            phaseBannerGroup.blocksRaycasts = false;
            phaseBannerGroup.interactable = false;
            phaseBannerGroup.alpha = 0f;
            phaseBanner = ui.Label("Phase Name", "", banner, 27,
                RuntimeUiFactory.Gold, new Vector2(820, 54), TextAnchor.MiddleCenter);

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

            tutorial = gameObject.AddComponent<KinTutorialPresenter>();
            tutorial.Initialize(ui, safeArea);
        }

        private GameObject FullScreenPanel(string name)
        {
            RectTransform panel = ui.Panel(name, safeArea,
                new Color(0.01f, 0.012f, 0.025f, 0.94f), Vector2.zero);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<HudReveal>();
            return panel.gameObject;
        }

        private void Refresh()
        {
            if (house != null) houseBarAnimation.SetValue(house.CurrentHealth, house.MaxHealth);
            if (kin != null) kinBarAnimation.SetValue(kin.CurrentHealth, kin.MaxHealth);
            if (energy != null) energyBarAnimation.SetValue(energy.Current, energy.Maximum);
            int coins = wallet != null ? wallet.Coins : 0;
            if (displayedCoins != coins)
            {
                if (displayedCoins != int.MinValue)
                {
                    int difference = coins - displayedCoins;
                    coinDelta.text = difference > 0 ? $"+{difference}" : difference.ToString();
                    coinDelta.color = difference > 0 ? new Color(0.25f, 0.45f, 0.08f)
                        : new Color(0.65f, 0.08f, 0.025f);
                    coinDeltaTime = 0.9f;
                    coinDelta.gameObject.SetActive(true);
                    coinMotion.Punch();
                }
                coinsLabel.text = $"MOEDAS  {coins}";
                displayedCoins = coins;
            }
            int enemies = EnemyRegistry.Count;
            if (displayedPhase != phase || displayedWave != wave || displayedEnemies != enemies)
            {
                stats.text = $"FASE  {phase}/{totalPhases}     ONDA  {wave}/{totalWaves}" +
                    $"     DEMÔNIOS  {enemies}";
                displayedPhase = phase;
                displayedWave = wave;
                displayedEnemies = enemies;
            }
            int energyPercent = energy != null
                ? Mathf.RoundToInt(energy.Normalized * 100f) : 0;
            bool ready = energy != null && energy.IsFull;
            if (displayedEnergy != energyPercent || displayedReady != ready)
            {
                specialLabel.text = ready ? "GOLPE\nPRONTO!" : $"GOLPE\n{energyPercent}%";
                if (ready && !displayedReady) specialMotion.Punch();
                displayedEnergy = energyPercent;
                displayedReady = ready;
            }
            specialButton.interactable = ready && !ended && !paused;
            specialMotion.SetReady(ready && !ended && !paused);
            energyRing.fillAmount = energyBar.fillAmount;
            energyRing.color = ready ? Color.Lerp(RuntimeUiFactory.Gold, Color.white,
                0.2f + Mathf.Sin(Time.unscaledTime * 4f) * 0.15f) : RuntimeUiFactory.Gold;
            countdown.gameObject.SetActive(preparing && !paused && !ended);
            if (preparing && displayedPreparation != preparation)
            {
                countdown.text = $"FASE {phase}\n{preparation}";
                displayedPreparation = preparation;
                countdownMotion.Punch();
            }

            for (int index = 0; index < 3; index++)
            {
                defenseMotions[index].SetSelected(
                    TowerBuildSelection.Selected == defenseTypes[index]);
                defenseButtons[index].interactable = !ended && !paused;
            }
        }

        private void AnimateAnnouncements()
        {
            if (coinDeltaTime > 0f)
            {
                coinDeltaTime = Mathf.Max(0, coinDeltaTime - Time.unscaledDeltaTime);
                ((RectTransform)coinDelta.transform).anchoredPosition =
                    new Vector2(0, -8f + (0.9f - coinDeltaTime) * 34f);
                Color color = coinDelta.color;
                color.a = Mathf.Clamp01(coinDeltaTime / 0.25f);
                coinDelta.color = color;
                if (coinDeltaTime <= 0f) coinDelta.gameObject.SetActive(false);
            }
            if (bannerTime > 0f)
            {
                bannerTime = Mathf.Max(0f, bannerTime - Time.unscaledDeltaTime);
                phaseBannerGroup.alpha = Mathf.Min(
                    Mathf.Clamp01((3f - bannerTime) / 0.25f),
                    Mathf.Clamp01(bannerTime / 0.5f));
            }
        }

        private void Announce(string message)
        {
            phaseBanner.text = message;
            phaseBannerGroup.alpha = 0f;
            bannerTime = 3f;
        }

        private void OnApplicationPause(bool backgrounded)
        {
            if (!backgrounded || paused || ended || rootCanvas == null ||
                !rootCanvas.gameObject.activeInHierarchy) return;
            paused = true;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            pauseLabel.text = "CONTINUAR";
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
            if (Screen.width <= 0 || Screen.height <= 0) return;
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

        private void OnPhase(int value, int total)
        {
            phase = value;
            totalPhases = total;
            wave = 0;
            Announce($"FASE {phase}  ·  {CampaignProgress.GetPhaseTitle(phase)}");
        }
        private void OnWave(int value, int total)
        {
            wave = value;
            totalWaves = total;
            Announce($"ONDA {wave}/{totalWaves}  ·  PROTEJA A CASA!");
        }
        private void OnPreparation(int value, int seconds)
        {
            preparing = true;
            wave = value;
            preparation = seconds;
        }
        private void OnPreparationEnded()
        {
            preparing = false;
            displayedPreparation = -1;
        }
        private void OnHouseDestroyed() => ShowResult("A CASA CAIU!",
            "Mesmo ferido, Kin fez tudo o que pôde.", "EndingDefeat");
        private void OnKinDown() => ShowResult("KIN FOI DERROTADO!",
            "Seu dono nunca saberá como Kin tentou protegê-lo.",
            "EndingDefeat");
        private void OnVictory() => ShowResult("A CASA ESTÁ SEGURA!",
            "O guardião da noite venceu mais uma batalha.", "EndingVictory");
    }
}
