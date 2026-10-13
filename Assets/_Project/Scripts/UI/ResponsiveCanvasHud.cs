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
        private BattleMinimap minimap;
        private Button specialButton;
        private Button pauseButton;
        private readonly Button[] defenseButtons = new Button[3];
        private readonly HudMotion[] defenseMotions = new HudMotion[3];
        private readonly Image[] defenseFrames = new Image[3];
        private readonly Image[] defenseArtwork = new Image[3];
        private readonly Text[] defensePrices = new Text[3];
        private readonly Image[] defenseBadges = new Image[3];
        private RectTransform defenseDock;
        private Image specialGlyph;
        private Text buildFeedback;
        private Button cancelConstruction;
        private CanvasGroup buildFeedbackGroup;
        private float buildFeedbackTime;
        private int constructionCoins = int.MinValue;
        private DefenseType constructionType = (DefenseType)(-1);
        private bool constructionPending;
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
            minimap = gameObject.AddComponent<BattleMinimap>();
            minimap.Initialize(ui, safeArea);
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
            KinGuideOverlay.Ensure();
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
            if (!KinGuideOverlay.IsOpen && !KinGuideOverlay.ConsumedInputThisFrame &&
                rootCanvas.gameObject.activeInHierarchy && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (TowerBuildSelection.AwaitingPlacement) TowerBuildSelection.CancelPlacement();
                else TogglePause();
            }
            if (!paused && !ended && !KinGuideOverlay.IsOpen && Time.timeScale > 0f &&
                rootCanvas.gameObject.activeInHierarchy && Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectDefense(0);
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectDefense(1);
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectDefense(2);
            }
            if (!paused && !ended && !KinGuideOverlay.IsOpen && !KinGuideOverlay.ConsumedInputThisFrame &&
                rootCanvas.gameObject.activeInHierarchy && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
                minimap.ToggleView();
            minimap.SetInteractionAllowed(!paused && !ended && rootCanvas.gameObject.activeInHierarchy);
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
        public bool CanGuideTutorial => !paused && !ended && !KinGuideOverlay.IsOpen && rootCanvas.gameObject.activeInHierarchy;
        public bool CanReplayTutorial => !paused && !ended && rootCanvas.gameObject.activeInHierarchy;

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
            BuildSpot.DefenseBuilt += OnDefenseBuilt;
            BuildSpot.BuildFailed += OnBuildFailed;
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
            BuildSpot.DefenseBuilt -= OnDefenseBuilt;
            BuildSpot.BuildFailed -= OnBuildFailed;
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
            RectTransform panel = ui.Surface("Garden Status", safeArea,
                new Color(RuntimeUiFactory.Ink.r, RuntimeUiFactory.Ink.g, RuntimeUiFactory.Ink.b, .94f), new Vector2(440, 176));
            Position(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22));
            panel.GetComponent<Image>().raycastTarget = false;
            AddBarRow(panel, "CASA", -18, out houseBar, RuntimeUiFactory.Coral, 14);
            AddBarRow(panel, "KIN", -46, out kinBar, new Color(.43f, .69f, .51f), 14);
            AddBarRow(panel, "ENERGIA", -74, out energyBar, RuntimeUiFactory.Gold, 10);
            ui.Rule(panel, 394, new Vector2(0, -12));
            Image coin = ui.Icon("Coins Symbol", UiIcon.Coin, panel, new Vector2(29, 29), RuntimeUiFactory.Gold);
            Position(coin.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -103));
            coinsLabel = ui.Label("Coins", "0", panel, 24, RuntimeUiFactory.Gold,
                new Vector2(300, 30), TextAnchor.MiddleLeft);
            RectTransform coinsRect = (RectTransform)coinsLabel.transform;
            Position(coinsRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(62, -103));
            coinMotion = coinsRect.gameObject.AddComponent<HudMotion>();
            coinDelta = ui.Label("Coin Reward", "", coinsRect, 19, RuntimeUiFactory.Paper,
                new Vector2(100, 28), TextAnchor.MiddleRight);
            coinDelta.gameObject.SetActive(false);
            stats = ui.Label("Stats", "", panel, 17, RuntimeUiFactory.Paper,
                new Vector2(398, 30), TextAnchor.MiddleLeft);
            Position(stats.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -139));

            // Only the three round artworks and small name/price tags occupy the dock.
            // This root has no background, header, instruction paragraph or rectangular cards.
            defenseDock = ui.Rect("Defense Icons", safeArea);
            defenseDock.sizeDelta = new Vector2(528, 176);
            Position(defenseDock, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 28));
            string[] labels = { "PORTAL", "BONSAI", "LANTERNA" };
            string[] artwork = { "TowerPortal", "TowerBonsai", "TowerLantern" };
            for (int index = 0; index < defenseButtons.Length; index++)
                AddDefenseButton(defenseDock, index, labels[index], artwork[index]);
            cancelConstruction = ui.Button("Cancel Construction", "CANCELAR", defenseDock,
                RuntimeUiFactory.Ink, new Vector2(174, 48));
            Position((RectTransform)cancelConstruction.transform, new Vector2(1, .5f), new Vector2(0, .5f), new Vector2(16, -30));
            Text cancelLabel = cancelConstruction.GetComponentInChildren<Text>();
            cancelLabel.fontSize = cancelLabel.resizeTextMaxSize = 17;
            cancelConstruction.onClick.AddListener(() => { if (!KinGuideOverlay.IsOpen && !KinGuideOverlay.ConsumedInputThisFrame) TowerBuildSelection.CancelPlacement(); });
            cancelConstruction.gameObject.SetActive(false);

            RectTransform feedbackPanel = ui.Surface("Construction Feedback", safeArea, RuntimeUiFactory.Paper, new Vector2(620, 56));
            Position(feedbackPanel, new Vector2(.6f, 1), new Vector2(.5f, 1), new Vector2(0, -92));
            feedbackPanel.GetComponent<Image>().raycastTarget = false;
            buildFeedbackGroup = feedbackPanel.gameObject.AddComponent<CanvasGroup>();
            buildFeedbackGroup.blocksRaycasts = false; buildFeedbackGroup.interactable = false; buildFeedbackGroup.alpha = 0;
            buildFeedback = ui.Label("Build Result", "", feedbackPanel, 22, RuntimeUiFactory.Ink,
                new Vector2(574, 48), TextAnchor.MiddleCenter);
        }

        private void AddDefenseButton(RectTransform panel, int index, string label, string artwork)
        {
            int captured = index;
            Button button = ui.ClearButton($"Defense {index}", panel, new Vector2(152, 176));
            defenseButtons[index] = button; defenseMotions[index] = button.GetComponent<HudMotion>();
            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.anchoredPosition = new Vector2((index - 1) * 174, 0);
            Image shadow = ui.Disc("Icon Shadow", buttonRect, new Vector2(118, 118), new Color(.02f, .05f, .04f, .38f));
            shadow.rectTransform.anchoredPosition = new Vector2(0, 28);
            Image rim = ui.Disc("Selection Ring", buttonRect, new Vector2(118, 118), RuntimeUiFactory.Gold, true);
            rim.rectTransform.anchoredPosition = new Vector2(0, 33); defenseFrames[index] = rim;
            Image well = ui.Disc("Icon Well", buttonRect, new Vector2(106, 106), RuntimeUiFactory.Ink);
            well.rectTransform.anchoredPosition = new Vector2(0, 33);
            RectTransform maskRect = ui.Rect("Artwork Mask", well.transform); maskRect.sizeDelta = new Vector2(100, 100);
            Image maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.sprite = RuntimeUiFactory.CircleSprite; maskImage.raycastTarget = false;
            Mask mask = maskRect.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            Texture2D texture = LoadArtwork(artwork);
            if (texture != null)
            {
                RectTransform iconRect = ui.Rect("Artwork", maskRect); RuntimeUiFactory.Stretch(iconRect, Vector2.zero);
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                icon.raycastTarget = false; defenseArtwork[index] = icon;
                ownedArtwork.Add(icon.sprite); if (texture.name == $"{artwork}_Hud") ownedArtwork.Add(texture);
            }
            RectTransform nameTag = ui.Panel("Defense Name Tag", buttonRect, new Color(.055f, .13f, .1f, .93f), new Vector2(140, 28));
            nameTag.anchoredPosition = new Vector2(0, -37); nameTag.GetComponent<Image>().raycastTarget = false;
            ui.Label("Defense Name", label, nameTag, 18, RuntimeUiFactory.Paper, new Vector2(130, 26), TextAnchor.MiddleCenter);
            RectTransform price = ui.Panel("Price Tag", buttonRect, RuntimeUiFactory.Ink, new Vector2(140, 30));
            price.anchoredPosition = new Vector2(0, -69); price.GetComponent<Image>().raycastTarget = false;
            Image coin = ui.Icon("Price Coin", UiIcon.Coin, price, new Vector2(22, 22), RuntimeUiFactory.Gold);
            coin.rectTransform.anchoredPosition = new Vector2(-47, 0);
            defensePrices[index] = ui.Label("Cost", "", price, 18, RuntimeUiFactory.Gold,
                new Vector2(105, 26), TextAnchor.MiddleCenter);
            defensePrices[index].rectTransform.anchoredPosition = new Vector2(13, 0);
            RectTransform keyTag = ui.Panel("Shortcut Tag", buttonRect, RuntimeUiFactory.Ink, new Vector2(27, 26));
            keyTag.anchoredPosition = new Vector2(-46, 76); keyTag.GetComponent<Image>().raycastTarget = false;
            ui.Label("Shortcut", (index + 1).ToString(), keyTag, 16, RuntimeUiFactory.Paper, new Vector2(25, 24), TextAnchor.MiddleCenter);
            keyTag.gameObject.SetActive(!Application.isMobilePlatform);
            Image badge = ui.Disc("Selected Seal", buttonRect, new Vector2(30, 30), RuntimeUiFactory.Gold);
            badge.rectTransform.anchoredPosition = new Vector2(46, 76);
            defenseBadges[index] = ui.Icon("Selected Check", UiIcon.Check, badge.transform, new Vector2(24, 24), RuntimeUiFactory.Ink);
            // Keep the whole seal under the selection state, including its filled backing.
            badge.gameObject.SetActive(false);
            button.onClick.AddListener(() => SelectDefense(captured));
        }

        private void SelectDefense(int index)
        {
            if (paused || ended || KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame || Time.timeScale <= 0f) return;
            TowerBuildSelection.Select(defenseTypes[index]);
            int coins = wallet != null ? wallet.Coins : 0;
            int missing = Mathf.Max(0, TowerBuildSelection.GetCost(defenseTypes[index]) - coins);
            ShowBuildFeedback(missing > 0 ? "Faltam " + missing + " moedas. Derrote demônios para construir."
                : TowerBuildSelection.GetDisplayName(defenseTypes[index]) + " • escolha uma base iluminada.", missing > 0);
            defenseMotions[index].Punch();
        }

        private void RefreshConstruction(int coins)
        {
            bool choosing = TowerBuildSelection.AwaitingPlacement;
            DefenseType type = TowerBuildSelection.Selected;
            if (constructionCoins != coins || constructionType != type || constructionPending != choosing)
            {
                constructionCoins = coins; constructionType = type; constructionPending = choosing;
                cancelConstruction.gameObject.SetActive(choosing);
                for (int index = 0; index < 3; index++)
                {
                    int price = TowerBuildSelection.GetCost(defenseTypes[index]);
                    bool affordable = coins >= price;
                    defensePrices[index].text = affordable ? price.ToString() : "FALTAM " + (price - coins);
                    defensePrices[index].color = affordable ? RuntimeUiFactory.Gold : new Color(1f, .64f, .49f);
                    if (defenseArtwork[index] != null) defenseArtwork[index].color = new Color(1, 1, 1, affordable ? 1f : 0.52f);
                }
            }
            for (int index = 0; index < 3; index++)
            {
                bool selected = choosing && type == defenseTypes[index];
                defenseMotions[index].SetSelected(selected);
                defenseMotions[index].SetReady(selected && coins >= TowerBuildSelection.GetCost(type) && !paused && !ended);
                defenseFrames[index].color = selected
                    ? Color.Lerp(RuntimeUiFactory.Gold, Color.white, 0.12f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f)
                    : new Color(RuntimeUiFactory.Gold.r, RuntimeUiFactory.Gold.g, RuntimeUiFactory.Gold.b, .45f);
                defenseBadges[index].transform.parent.gameObject.SetActive(selected);
                defenseButtons[index].interactable = !ended && !paused && !KinGuideOverlay.IsOpen;
            }
        }

        private void OnDefenseBuilt(DefenseType type, GameObject defense)
        {
            if (defense == null) return;
            string message = type == DefenseType.Portal
                ? Object.FindObjectsByType<PortalTransport>(FindObjectsSortMode.None).Length < 2
                    ? "Portal construído • crie outro para conectar o caminho."
                    : "Portais conectados • aproxime Kin para viajar."
                : TowerBuildSelection.GetDisplayName(type) + (type == DefenseType.Lantern ? " construída" : " construído") +
                    " • aproxime Kin para usar seu poder.";
            ShowBuildFeedback(message, false);
        }

        private void OnBuildFailed(BuildSpot spot, DefenseType type, BuildFailure reason)
        {
            string message = reason switch
            {
                BuildFailure.ChooseDefense => "Primeiro, escolha uma defesa na faixa inferior.",
                BuildFailure.Occupied => "Esta base já tem uma defesa. Escolha um anel livre.",
                _ => "Faltam " + Mathf.Max(0, TowerBuildSelection.GetCost(type) - (wallet != null ? wallet.Coins : 0)) + " moedas para construir."
            };
            spot.GetComponent<BuildSpotHint>()?.PlayErrorPulse();
            ShowBuildFeedback(message, true);
        }

        private void ShowBuildFeedback(string message, bool error)
        {
            buildFeedback.text = message;
            buildFeedback.color = error ? RuntimeUiFactory.Coral : RuntimeUiFactory.Jade;
            buildFeedbackTime = 2.4f;
            buildFeedbackGroup.alpha = 1f;
        }

        private void AddBarRow(RectTransform panel, string name, float y,
            out Image bar, Color color, float height)
        {
            Text label = ui.Label(name, name, panel, 17, RuntimeUiFactory.Paper,
                new Vector2(88, 25), TextAnchor.MiddleLeft);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(22, y);
            bar = ui.Bar($"{name} Bar", panel, new Vector2(280, height));
            bar.color = color;
            RectTransform barRect = (RectTransform)bar.transform.parent;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(128, y - 5);
        }

        private void BuildActions()
        {
            Vector2 specialAnchor = new(.86f, 0), specialPivot = new(.5f, 0), specialPosition = new(0, 38);
            CreateCircleLayer("Special Shadow", safeArea, specialAnchor, specialPivot,
                specialPosition - new Vector2(0, 5), new Vector2(164, 164), new Color(.02f, .05f, .04f, .38f));
            Image rim = CreateCircleLayer("Special Fine Rim", safeArea, specialAnchor, specialPivot,
                specialPosition, new Vector2(162, 162), new Color(RuntimeUiFactory.Gold.r, RuntimeUiFactory.Gold.g, RuntimeUiFactory.Gold.b, .35f));
            rim.sprite = RuntimeUiFactory.RingSprite;
            energyRing = CreateCircleLayer("Energy Charge Ring", safeArea, specialAnchor, specialPivot,
                specialPosition, new Vector2(166, 166), RuntimeUiFactory.Gold);
            energyRing.sprite = RuntimeUiFactory.RingSprite; energyRing.type = Image.Type.Filled;
            energyRing.fillMethod = Image.FillMethod.Radial360; energyRing.fillOrigin = (int)Image.Origin360.Top;
            energyRing.fillClockwise = true; energyRing.fillAmount = 0;
            specialButton = ui.Button("Special", "GOLPE\n0%", safeArea, RuntimeUiFactory.Jade, new Vector2(144, 144));
            RectTransform specialRect = (RectTransform)specialButton.transform;
            Position(specialRect, specialAnchor, specialPivot, specialPosition + new Vector2(0, 9));
            Image specialImage = specialButton.GetComponent<Image>(); specialImage.sprite = RuntimeUiFactory.CircleSprite; specialImage.type = Image.Type.Simple;
            Image contour = specialRect.Find("Fine Gold Contour").GetComponent<Image>();
            contour.sprite = RuntimeUiFactory.RingSprite; contour.type = Image.Type.Simple; contour.color = new Color(1, 1, 1, .12f);
            ColorBlock colors = specialButton.colors; colors.disabledColor = new Color(.85f, .9f, .85f, 1); specialButton.colors = colors;
            specialLabel = specialButton.GetComponentInChildren<Text>(); specialLabel.color = RuntimeUiFactory.Paper;
            specialLabel.fontSize = specialLabel.resizeTextMaxSize = 20; specialLabel.lineSpacing = .9f;
            specialLabel.rectTransform.sizeDelta = new Vector2(122, 55); specialLabel.rectTransform.anchoredPosition = new Vector2(0, -36);
            specialGlyph = ui.Icon("Sword Symbol", UiIcon.Special, specialRect, new Vector2(66, 66), RuntimeUiFactory.Gold);
            specialGlyph.rectTransform.anchoredPosition = new Vector2(0, 21);
            specialMotion = specialButton.GetComponent<HudMotion>();
            specialButton.onClick.AddListener(() => { if (!KinGuideOverlay.IsOpen && !KinGuideOverlay.ConsumedInputThisFrame) special?.TryUse(); });
            Text shortcut = ui.Label("Special Shortcut", "ESPAÇO", specialRect, 16, RuntimeUiFactory.Paper, new Vector2(130, 28), TextAnchor.MiddleCenter);
            shortcut.rectTransform.anchoredPosition = new Vector2(0, -93); shortcut.gameObject.SetActive(!Application.isMobilePlatform);

            pauseButton = ui.Button("Pause", "PAUSAR", safeArea, RuntimeUiFactory.Ink, new Vector2(176, 62));
            RectTransform pauseRect = (RectTransform)pauseButton.transform;
            Position(pauseRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -26));
            pauseLabel = pauseButton.GetComponentInChildren<Text>(); pauseLabel.color = RuntimeUiFactory.Paper;
            pauseLabel.fontSize = pauseLabel.resizeTextMaxSize = 18; pauseLabel.rectTransform.anchoredPosition = new Vector2(18, 0);
            pauseLabel.rectTransform.sizeDelta = new Vector2(120, 48);
            ui.Icon("Pause Symbol", UiIcon.Pause, pauseRect, new Vector2(28, 28), RuntimeUiFactory.Gold).rectTransform.anchoredPosition = new Vector2(-54, 0);
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
            countdown = ui.Label("Countdown", "", safeArea, 64, RuntimeUiFactory.Paper,
                new Vector2(600, 170), TextAnchor.MiddleCenter);
            countdown.gameObject.SetActive(false); countdown.raycastTarget = false;
            countdownMotion = countdown.gameObject.AddComponent<HudMotion>();
            countdown.gameObject.AddComponent<Shadow>().effectColor = RuntimeUiFactory.Ink;
            RectTransform banner = ui.Surface("Phase Banner", safeArea, RuntimeUiFactory.Ink, new Vector2(620, 54));
            Position(banner, new Vector2(.6f, 1), new Vector2(.5f, 1), new Vector2(0, -26));
            banner.GetComponent<Image>().raycastTarget = false;
            phaseBannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            phaseBannerGroup.blocksRaycasts = false; phaseBannerGroup.interactable = false; phaseBannerGroup.alpha = 0;
            phaseBanner = ui.Label("Phase Name", "", banner, 23, RuntimeUiFactory.Gold,
                new Vector2(574, 48), TextAnchor.MiddleCenter);

            pausePanel = FullScreenPanel("Pause Panel");
            RectTransform pauseCard = ui.Surface("Pause Book", pausePanel.transform, RuntimeUiFactory.Paper, new Vector2(640, 540), true);
            RuntimeUiFactory.Fit(pauseCard, new Vector2(80, 80));
            ui.Icon("Pause Crest", UiIcon.Pause, pauseCard, new Vector2(54, 54), RuntimeUiFactory.Jade).rectTransform.anchoredPosition = new Vector2(0, 180);
            Text pausedTitle = ui.Label("Pause Title", "JARDIM EM PAUSA", pauseCard, 34, RuntimeUiFactory.Ink, new Vector2(560, 64), TextAnchor.MiddleCenter);
            pausedTitle.rectTransform.anchoredPosition = new Vector2(0, 98);
            ui.Rule(pauseCard, 480, new Vector2(0, 46));
            Button resume = ui.Button("Resume", "CONTINUAR", pauseCard, RuntimeUiFactory.Jade, new Vector2(452, 72));
            ((RectTransform)resume.transform).anchoredPosition = new Vector2(0, -18); resume.onClick.AddListener(TogglePause);
            Button guide = ui.Button("Pause Guide", "GUIA DO KIN", pauseCard, RuntimeUiFactory.Ink, new Vector2(452, 66));
            ((RectTransform)guide.transform).anchoredPosition = new Vector2(0, -106); guide.onClick.AddListener(() => KinGuideOverlay.OpenGuide());
            Button restart = ui.Button("Restart", "REINICIAR PARTIDA", pauseCard, RuntimeUiFactory.Ink, new Vector2(452, 66));
            ((RectTransform)restart.transform).anchoredPosition = new Vector2(0, -190); restart.onClick.AddListener(Restart);
            pausePanel.SetActive(false);

            resultPanel = FullScreenPanel("Result Panel"); resultBackground = resultPanel.GetComponent<Image>();
            resultBackground.type = Image.Type.Simple; resultBackground.preserveAspect = false;
            RectTransform shadeRect = ui.Panel("Result Shade", resultPanel.transform, new Color(.025f, .07f, .055f, .42f), Vector2.zero);
            RuntimeUiFactory.Stretch(shadeRect, Vector2.zero); shadeRect.GetComponent<Image>().raycastTarget = false;
            RectTransform resultCard = ui.Surface("Result Book", resultPanel.transform, new Color(.075f, .16f, .15f, .96f), new Vector2(1100, 430), true);
            RuntimeUiFactory.Fit(resultCard, new Vector2(100, 120)); resultCard.anchoredPosition = new Vector2(0, -120);
            resultTitle = ui.Label("Result Title", "", resultCard, 42, RuntimeUiFactory.Gold, new Vector2(980, 80), TextAnchor.MiddleCenter);
            resultTitle.rectTransform.anchoredPosition = new Vector2(0, 112);
            ui.Rule(resultCard, 900, new Vector2(0, 46));
            resultMessage = ui.Label("Result Message", "", resultCard, 27, RuntimeUiFactory.Paper, new Vector2(960, 80), TextAnchor.MiddleCenter);
            resultMessage.fontStyle = FontStyle.Normal; resultMessage.rectTransform.anchoredPosition = new Vector2(0, -12);
            Button resultRestart = ui.Button("Result Restart", "JOGAR NOVAMENTE", resultCard, RuntimeUiFactory.Gold, new Vector2(400, 72));
            ((RectTransform)resultRestart.transform).anchoredPosition = new Vector2(-228, -130); resultRestart.onClick.AddListener(Restart);
            Button menu = ui.Button("Result Menu", "MENU PRINCIPAL", resultCard, RuntimeUiFactory.Jade, new Vector2(400, 72));
            ((RectTransform)menu.transform).anchoredPosition = new Vector2(228, -130);
            menu.onClick.AddListener(() => { if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return; Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); });
            resultPanel.SetActive(false);
            tutorial = gameObject.AddComponent<KinTutorialPresenter>(); tutorial.Initialize(ui, safeArea);
        }

        private GameObject FullScreenPanel(string name)
        {
            RectTransform panel = ui.Panel(name, safeArea,
                RuntimeUiFactory.Veil, Vector2.zero);
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
                    coinDelta.color = difference > 0 ? RuntimeUiFactory.Gold
                        : new Color(.98f, .63f, .49f);
                    coinDeltaTime = 0.9f;
                    coinDelta.gameObject.SetActive(true);
                    coinMotion.Punch();
                }
                coinsLabel.text = coins.ToString();
                displayedCoins = coins;
            }
            int enemies = EnemyRegistry.Count;
            if (displayedPhase != phase || displayedWave != wave || displayedEnemies != enemies)
            {
                stats.text = $"FASE  {phase}/{totalPhases}     ONDA  {wave}/{totalWaves}" +
                    $"     INIMIGOS  {enemies}";
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

            RefreshConstruction(coins);
        }

        private void AnimateAnnouncements()
        {
            if (buildFeedbackTime > 0f)
            {
                buildFeedbackTime = Mathf.Max(0f, buildFeedbackTime - Time.unscaledDeltaTime);
                buildFeedbackGroup.alpha = Mathf.Clamp01(buildFeedbackTime / 0.35f);
            }
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
            if (ended || KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;

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
            if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;
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
            Canvas.ForceUpdateCanvases();
            bool compact = safeArea.rect.width < 1650f;
            if (cancelConstruction != null)
                Position((RectTransform)cancelConstruction.transform,
                    compact ? new Vector2(.5f, 1) : new Vector2(1, .5f),
                    compact ? new Vector2(.5f, 0) : new Vector2(0, .5f),
                    compact ? new Vector2(0, 16) : new Vector2(16, -30));
        }

        private void OnPhase(int value, int total)
        {
            TowerBuildSelection.CancelPlacement();
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
