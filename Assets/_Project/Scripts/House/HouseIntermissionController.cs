using System.Collections.Generic;
using System.Linq;
using CatsVsDemons.CameraSystem;
using CatsVsDemons.Economy;
using CatsVsDemons.Player;
using CatsVsDemons.UI;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.House
{
    public sealed class HouseIntermissionController : MonoBehaviour
    {
        private const int KinPreviewLayer = 30;

        private readonly Dictionary<GameObject, bool> battleStates = new();

        private EnemyWaveSpawner waves;
        private Wallet wallet;
        private ResponsiveCanvasHud battleHud;
        private CameraModeController cameraController;
        private Camera gameCamera;
        private KinHealth kinHealth;
        private KinEnergy kinEnergy;
        private KinPrototypeAttack kinAttack;
        private KinSpecialAttack kinSpecialAttack;
        private KinShopLoadout kinLoadout;

        private RuntimeUiFactory ui;
        private GameObject interiorRoot;
        private GameObject interfaceRoot;
        private GameObject shopPanel;
        private RectTransform safeArea;
        private RectTransform houseBackgroundRect;
        private RectTransform catalogFrame;
        private RectTransform catalogArtworkRect;
        private RectTransform catalogOverlay;
        private RawImage catalogArtwork;
        private Text phaseTitle;
        private Text phaseMessage;
        private Text coinsLabel;
        private Text shopCoinsLabel;
        private Text selectedName;
        private Text selectedDescription;
        private Text shopStatus;
        private readonly Button[] categoryButtons = new Button[3];
        private readonly Dictionary<HouseShopItem, Button> catalogButtons = new();
        private Button shopActionButton;
        private Text shopActionLabel;
        private Text houseInstruction;
        private Button houseContinueButton;
        private Text houseContinueLabel;
        private RawImage kinPreviewImage;
        private Image kinHealthProgress;
        private Image kinEnergyProgress;
        private Image kinAttackProgress;
        private Image kinSpecialProgress;
        private Text kinLoadoutLabel;
        private Text kinHealthValue;
        private Text kinEnergyValue;
        private Text kinAttackValue;
        private Text kinSpecialValue;
        private Rect lastSafeArea;
        private bool isOpen;
        private bool openedFromPause;

        private Texture2D houseArtwork;
        private Texture2D clothesArtwork;
        private Texture2D accessoriesArtwork;
        private HouseShopCategory selectedCategory;
        private HouseShopItem selectedItem;
        private GameObject kinPreviewRoot;
        private Camera kinPreviewCamera;
        private RenderTexture kinPreviewTexture;
        // The preview camera is on Kin's left/front side. 180 degrees makes
        // the first view show his face instead of his back.
        private float kinPreviewYaw = 180f;

        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private bool savedOrthographic;
        private float savedOrthographicSize;
        private float savedFieldOfView;
        private CameraClearFlags savedClearFlags;
        private Color savedBackgroundColor;
        private bool savedCameraControllerEnabled;
        private bool cameraStateCaptured;

        public void Initialize(EnemyWaveSpawner source)
        {
            if (waves == source)
            {
                return;
            }

            if (waves != null)
            {
                waves.IntermissionStarted -= EnterIntermission;
            }

            waves = source;
            if (waves != null)
            {
                waves.IntermissionStarted += EnterIntermission;
            }
        }

        private void Awake()
        {
            Transform parent = GameObject.Find("Game")?.transform;
            houseArtwork = Resources.Load<Texture2D>("UI/HouseInterior");
            clothesArtwork = Resources.Load<Texture2D>("UI/HouseShopClothes");
            accessoriesArtwork =
                Resources.Load<Texture2D>("UI/HouseShopAccessories");

            if (houseArtwork == null)
            {
                interiorRoot = HouseInteriorBuilder.Build(parent);
            }
            else
            {
                interiorRoot = new GameObject("House 3D Fallback");
                interiorRoot.transform.SetParent(parent, false);
            }
            interiorRoot.SetActive(false);
            BuildInterface();
        }

        private void Start()
        {
            if (waves == null)
            {
                Initialize(Object.FindFirstObjectByType<EnemyWaveSpawner>());
            }

            FindSceneReferences();
            ApplyPurchasedBenefits();
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }

            UpdateCameraFraming();
            UpdateCoinLabels();
            RefreshKinProfile();
        }

        private void OnDestroy()
        {
            if (waves != null)
            {
                waves.IntermissionStarted -= EnterIntermission;
            }

            if (isOpen)
            {
                RestoreBattlefield();
                RestoreCamera();
            }

            DestroyKinPreview();
            if (openedFromPause)
            {
                Time.timeScale = 1f;
            }
        }

        private void FindSceneReferences()
        {
            waves ??= Object.FindFirstObjectByType<EnemyWaveSpawner>();
            wallet ??= Object.FindFirstObjectByType<Wallet>();
            battleHud ??= Object.FindFirstObjectByType<ResponsiveCanvasHud>();
            cameraController ??=
                Object.FindFirstObjectByType<CameraModeController>();
            gameCamera ??= Camera.main;

            if (kinHealth != null && kinEnergy != null &&
                kinAttack != null && kinSpecialAttack != null &&
                kinLoadout != null)
            {
                return;
            }

            GameObject kin = GameObject.FindGameObjectWithTag("Player");
            if (kin == null)
            {
                return;
            }

            kinHealth = kin.GetComponent<KinHealth>();
            kinEnergy = kin.GetComponent<KinEnergy>();
            kinAttack = kin.GetComponent<KinPrototypeAttack>();
            kinSpecialAttack = kin.GetComponent<KinSpecialAttack>();
            kinLoadout = kin.GetComponent<KinShopLoadout>();
            if (kinLoadout == null)
            {
                kinLoadout = kin.AddComponent<KinShopLoadout>();
            }
        }

        private void ApplyPurchasedBenefits()
        {
            FindSceneReferences();
            kinHealth?.SetShopMaximumBonus(
                HouseShopCatalog.GetPurchasedBonus(
                    HouseShopStat.MaximumHealth));
            kinEnergy?.SetShopMaximumBonus(
                HouseShopCatalog.GetPurchasedBonus(
                    HouseShopStat.MaximumEnergy));
            kinAttack?.SetShopDamageBonus(
                HouseShopCatalog.GetPurchasedBonus(
                    HouseShopStat.AttackDamage));
            kinSpecialAttack?.SetShopDamageBonus(
                HouseShopCatalog.GetPurchasedBonus(
                    HouseShopStat.SpecialDamage));
            kinLoadout?.Apply(
                HouseShopSave.GetEquipped(HouseShopCategory.Clothes),
                HouseShopSave.GetEquipped(HouseShopCategory.Accessories));
        }

        private void EnterIntermission(int completedPhase, int totalPhases)
        {
            if (isOpen)
            {
                return;
            }

            FindSceneReferences();
            ApplyPurchasedBenefits();
            openedFromPause = false;
            isOpen = true;
            phaseTitle.text = $"FASE {completedPhase} CONCLUÍDA";
            int nextPhase = completedPhase + 1;
            bool changingScenario =
                CampaignProgress.IsScenarioTransitionAfter(completedPhase);
            string nextDestination = CampaignProgress.GetPhaseTitle(nextPhase);
            phaseMessage.text = changingScenario
                ? $"A casa está segura. O próximo portal leva para " +
                  $"{nextDestination}: fase {nextPhase} de {totalPhases}."
                : $"A casa está segura. Próxima etapa: {nextDestination} " +
                  $"(fase {nextPhase} de {totalPhases}).";
            ConfigureHouseControls(
                "PRÓXIMA\nFASE",
                "Equipe Kin na loja e prepare-se para a próxima noite.");
            UpdateCoinLabels();
            kinPreviewYaw = 180f;
            RebuildKinPreview();
            RefreshKinProfile();

            CaptureAndHideBattlefield();
            ConfigureHouseCamera();
            interiorRoot.SetActive(houseArtwork == null);
            shopPanel.SetActive(false);
            interfaceRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        public bool OpenPauseRoom()
        {
            if (isOpen)
            {
                return false;
            }

            FindSceneReferences();
            if (waves == null || waves.IsInIntermission)
            {
                return false;
            }

            ApplyPurchasedBenefits();
            openedFromPause = true;
            isOpen = true;
            phaseTitle.text = "PAUSA NA CASA";
            phaseMessage.text =
                "Kin confere os equipamentos enquanto a batalha espera.";
            ConfigureHouseControls(
                "CONTINUAR\nBATALHA",
                "Visite a loja, confira o equipamento e volte ao jardim.");
            UpdateCoinLabels();
            kinPreviewYaw = 180f;
            RebuildKinPreview();
            RefreshKinProfile();

            Time.timeScale = 0f;
            battleHud?.SetHousePauseState(true);
            CaptureAndHideBattlefield();
            ConfigureHouseCamera();
            interiorRoot.SetActive(houseArtwork == null);
            shopPanel.SetActive(false);
            interfaceRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private void ContinueToNextPhase()
        {
            if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;
            if (!isOpen)
            {
                return;
            }

            bool resumeBattle = openedFromPause;
            shopPanel.SetActive(false);
            interfaceRoot.SetActive(false);
            interiorRoot.SetActive(false);
            DestroyKinPreview();
            RestoreBattlefield();
            RestoreCamera();
            isOpen = false;
            openedFromPause = false;
            battleHud?.SetHousePauseState(false);

            if (resumeBattle)
            {
                Time.timeScale = 1f;
                return;
            }

            waves?.ContinueFromHouse();
        }

        private void CaptureAndHideBattlefield()
        {
            battleStates.Clear();
            string[] paths =
            {
                "Game/Environment",
                "Game/Paths",
                "Game/BuildSpots",
                "Game/Enemies",
                "Game/Player"
            };

            foreach (string path in paths)
            {
                GameObject target = GameObject.Find(path);
                if (target == null)
                {
                    continue;
                }

                battleStates[target] = target.activeSelf;
                target.SetActive(false);
            }

            GameObject horizon = GameObject.Find("Runtime Horizon Ground");
            if (horizon != null)
            {
                battleStates[horizon] = horizon.activeSelf;
                horizon.SetActive(false);
            }

            battleHud?.SetHudVisible(false);
        }

        private void RestoreBattlefield()
        {
            foreach (KeyValuePair<GameObject, bool> state in battleStates)
            {
                if (state.Key != null)
                {
                    state.Key.SetActive(state.Value);
                }
            }
            battleStates.Clear();
            battleHud?.SetHudVisible(true);
        }

        private void ConfigureHouseCamera()
        {
            gameCamera ??= Camera.main;
            if (gameCamera == null)
            {
                return;
            }

            savedCameraPosition = gameCamera.transform.position;
            savedCameraRotation = gameCamera.transform.rotation;
            savedOrthographic = gameCamera.orthographic;
            savedOrthographicSize = gameCamera.orthographicSize;
            savedFieldOfView = gameCamera.fieldOfView;
            savedClearFlags = gameCamera.clearFlags;
            savedBackgroundColor = gameCamera.backgroundColor;
            savedCameraControllerEnabled =
                cameraController != null && cameraController.enabled;
            cameraStateCaptured = true;

            if (cameraController != null)
            {
                cameraController.enabled = false;
            }

            gameCamera.orthographic = true;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.008f, 0.018f, 0.055f);
            gameCamera.transform.position = new Vector3(-14.5f, 13.5f, -17.5f);
            gameCamera.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 2.1f, 0.2f) - gameCamera.transform.position,
                Vector3.up
            );
            UpdateCameraFraming();
        }

        private void UpdateCameraFraming()
        {
            if (gameCamera == null)
            {
                return;
            }

            float aspect = Mathf.Max(0.45f, gameCamera.aspect);
            gameCamera.orthographicSize = Mathf.Max(8.4f, 9.8f / aspect);
        }

        private void RestoreCamera()
        {
            if (!cameraStateCaptured || gameCamera == null)
            {
                return;
            }

            gameCamera.transform.position = savedCameraPosition;
            gameCamera.transform.rotation = savedCameraRotation;
            gameCamera.orthographic = savedOrthographic;
            gameCamera.orthographicSize = savedOrthographicSize;
            gameCamera.fieldOfView = savedFieldOfView;
            gameCamera.clearFlags = savedClearFlags;
            gameCamera.backgroundColor = savedBackgroundColor;

            if (cameraController != null)
            {
                cameraController.enabled = savedCameraControllerEnabled;
            }

            cameraStateCaptured = false;
        }

        private void BuildInterface()
        {
            ui = new RuntimeUiFactory();
            Canvas canvas = ui.CreateCanvas(transform);
            canvas.gameObject.name = "House Intermission UI";
            canvas.sortingOrder = 650;
            interfaceRoot = canvas.gameObject;
            interfaceRoot.AddComponent<HudReveal>();

            safeArea = ui.Rect("Safe Area", canvas.transform);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            BuildHouseArtwork();
            BuildHouseHud();
            BuildShopPanel();
            ApplySafeArea();
            interfaceRoot.SetActive(false);
        }

        private void BuildHouseArtwork()
        {
            RectTransform backdrop = ui.Panel(
                "House Night Backdrop",
                safeArea,
                RuntimeUiFactory.Ink,
                Vector2.zero
            );
            Stretch(backdrop);

            houseBackgroundRect = ui.Rect("House Interior Artwork", backdrop);
            houseBackgroundRect.sizeDelta = new Vector2(1536f, 1024f);
            RawImage background =
                houseBackgroundRect.gameObject.AddComponent<RawImage>();
            background.texture = houseArtwork;
            background.color = houseArtwork != null
                ? Color.white
                : new Color(0f, 0f, 0f, 0f);
            background.raycastTarget = false;

            AspectRatioFitter fitter =
                houseBackgroundRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1.5f;

            Button computer = ui.Button(
                "Computer Hotspot",
                "ABRIR LOJA",
                houseBackgroundRect,
                RuntimeUiFactory.Jade,
                new Vector2(210f, 66f)
            );
            RectTransform computerRect = (RectTransform)computer.transform;
            computerRect.anchorMin = computerRect.anchorMax =
                new Vector2(0.425f, 0.675f);
            computerRect.anchoredPosition = Vector2.zero;
            computer.onClick.AddListener(ShowShop);
        }

        private void BuildHouseHud()
        {
            RectTransform headerBorder = ui.Panel(
                "House Header Border",
                safeArea,
                new Color(.32f, .43f, .32f, .9f),
                new Vector2(810f, 122f)
            );
            headerBorder.anchorMin = headerBorder.anchorMax =
                new Vector2(0.5f, 1f);
            headerBorder.pivot = new Vector2(0.5f, 1f);
            headerBorder.anchoredPosition = new Vector2(0f, -18f);
            RuntimeUiFactory.Fit(headerBorder, new Vector2(660, 0));

            RectTransform header = ui.Panel(
                "House Header",
                headerBorder,
                RuntimeUiFactory.Ink,
                new Vector2(806f, 118f)
            );

            ui.Trim(header, RuntimeUiFactory.Gold);
            phaseTitle = ui.Label("Phase Title", "FASE CONCLUÍDA", header,
                38, RuntimeUiFactory.Gold, new Vector2(750f, 54f),
                TextAnchor.MiddleCenter);
            ((RectTransform)phaseTitle.transform).anchoredPosition =
                new Vector2(0f, 25f);
            phaseMessage = ui.Label("Phase Message", "", header, 20,
                Color.white, new Vector2(750f, 42f),
                TextAnchor.MiddleCenter);
            ((RectTransform)phaseMessage.transform).anchoredPosition =
                new Vector2(0f, -25f);

            RectTransform coinPanel = ui.Panel(
                "House Coins",
                safeArea,
                RuntimeUiFactory.Ink,
                new Vector2(300f, 70f)
            );
            coinPanel.anchorMin = coinPanel.anchorMax = new Vector2(0f, 1f);
            coinPanel.pivot = new Vector2(0f, 1f);
            coinPanel.anchoredPosition = new Vector2(24f, -24f);
            coinsLabel = ui.Label("Coins Label", "MOEDAS  0", coinPanel, 27,
                RuntimeUiFactory.Gold, new Vector2(268f, 54f),
                TextAnchor.MiddleCenter);

            houseInstruction = ui.Label(
                "House Instruction",
                "Equipe Kin na loja e prepare-se para a próxima noite.",
                safeArea,
                20,
                Color.white,
                new Vector2(850f, 50f),
                TextAnchor.MiddleCenter
            );
            RectTransform instructionRect =
                (RectTransform)houseInstruction.transform;
            instructionRect.anchorMin = instructionRect.anchorMax =
                new Vector2(0.5f, 0f);
            instructionRect.pivot = new Vector2(0.5f, 0f);
            instructionRect.anchoredPosition = new Vector2(0f, 25f);

            houseContinueButton = ui.Button(
                "Next Phase",
                "PRÓXIMA FASE",
                safeArea,
                RuntimeUiFactory.Gold,
                new Vector2(300f, 88f)
            );
            RectTransform nextRect =
                (RectTransform)houseContinueButton.transform;
            nextRect.anchorMin = nextRect.anchorMax = new Vector2(1f, 0f);
            nextRect.pivot = new Vector2(1f, 0f);
            nextRect.anchoredPosition = new Vector2(-24f, 24f);
            houseContinueButton.onClick.AddListener(ContinueToNextPhase);
            houseContinueLabel = houseContinueButton.GetComponentInChildren<Text>();

            BuildKinProfilePanel();
        }

        private void ConfigureHouseControls(string continueLabel,
            string instruction)
        {
            if (houseContinueLabel != null)
            {
                houseContinueLabel.text = continueLabel;
            }

            if (houseInstruction != null)
            {
                houseInstruction.text = instruction;
            }
        }

        private void BuildKinProfilePanel()
        {
            RectTransform border = ui.Panel(
                "Kin Profile Border",
                safeArea,
                new Color(.32f, .43f, .32f, .9f),
                new Vector2(452f, 694f)
            );
            border.anchorMin = border.anchorMax = new Vector2(0f, 0f);
            border.pivot = new Vector2(0f, 0f);
            border.anchoredPosition = new Vector2(24f, 84f);

            RectTransform panel = ui.Panel(
                "Kin Profile",
                border,
                RuntimeUiFactory.Ink,
                new Vector2(448f, 690f)
            );
            ui.Trim(panel, RuntimeUiFactory.Gold);
            RuntimeUiFactory.Fit(border, new Vector2(850, 290));
            // Keep every stat and value inside the profile card on narrower
            // displays as an additional safeguard to their fixed layout.
            panel.gameObject.AddComponent<RectMask2D>();

            Text title = ui.Label("Kin Profile Title", "KIN EQUIPADO",
                panel, 30, RuntimeUiFactory.Gold, new Vector2(404f, 42f),
                TextAnchor.MiddleCenter);
            ((RectTransform)title.transform).anchoredPosition =
                new Vector2(0f, 303f);

            kinLoadoutLabel = ui.Label("Kin Loadout", "", panel, 19,
                Color.white, new Vector2(404f, 48f),
                TextAnchor.MiddleCenter);
            ((RectTransform)kinLoadoutLabel.transform).anchoredPosition =
                new Vector2(0f, 256f);

            RectTransform portraitFrame = ui.Panel(
                "Kin Preview Frame",
                panel,
                new Color(.13f, .23f, .21f, 1),
                new Vector2(370f, 266f)
            );
            portraitFrame.anchoredPosition = new Vector2(0f, 96f);
            RectTransform portrait = ui.Rect("Kin Preview", portraitFrame);
            portrait.sizeDelta = new Vector2(354f, 250f);
            kinPreviewImage = portrait.gameObject.AddComponent<RawImage>();
            kinPreviewImage.color = Color.white;
            kinPreviewImage.raycastTarget = false;

            Button rotatePreview = ui.Button(
                "Rotate Kin Preview",
                "GIRAR",
                portraitFrame,
                RuntimeUiFactory.Jade,
                new Vector2(124f, 50f)
            );
            RectTransform rotateRect = (RectTransform)rotatePreview.transform;
            rotateRect.anchorMin = rotateRect.anchorMax =
                new Vector2(1f, 0f);
            rotateRect.pivot = new Vector2(1f, 0f);
            rotateRect.anchoredPosition = new Vector2(-10f, 10f);
            rotatePreview.onClick.AddListener(RotateKinPreview);

            kinHealthProgress = CreateKinProgressBar(panel, "VIDA", -80f,
                RuntimeUiFactory.Coral, out kinHealthValue);
            kinEnergyProgress = CreateKinProgressBar(panel, "ENERGIA", -150f,
                new Color(.43f, .69f, .51f), out kinEnergyValue);
            kinAttackProgress = CreateKinProgressBar(panel, "ATAQUE", -220f,
                RuntimeUiFactory.Gold, out kinAttackValue);
            kinSpecialProgress = CreateKinProgressBar(panel, "GOLPE", -290f,
                new Color(.51f, .66f, .72f), out kinSpecialValue);
        }

        private Image CreateKinProgressBar(Transform parent, string label,
            float verticalPosition, Color color, out Text value)
        {
            Text labelText = ui.Label($"{label} Label", label, parent, 18,
                Color.white, new Vector2(122f, 24f), TextAnchor.MiddleLeft);
            ((RectTransform)labelText.transform).anchoredPosition =
                new Vector2(-132f, verticalPosition);

            value = ui.Label($"{label} Value", "", parent, 18,
                RuntimeUiFactory.Gold, new Vector2(122f, 24f),
                TextAnchor.MiddleRight);
            ((RectTransform)value.transform).anchoredPosition =
                new Vector2(132f, verticalPosition);

            Image fill = ui.Bar($"{label} Progress", parent,
                new Vector2(362f, 22f));
            ((RectTransform)fill.transform.parent).anchoredPosition =
                new Vector2(0f, verticalPosition - 25f);
            fill.color = color;
            fill.fillAmount = 0f;
            return fill;
        }

        private void RefreshKinProfile()
        {
            int health = kinHealth != null ? kinHealth.CurrentHealth : 0;
            int maximumHealth = kinHealth != null ? kinHealth.MaxHealth : 0;
            int energy = kinEnergy != null
                ? Mathf.RoundToInt(kinEnergy.Current)
                : 0;
            int maximumEnergy = kinEnergy != null
                ? Mathf.RoundToInt(kinEnergy.Maximum)
                : 0;
            int attack = kinAttack != null ? kinAttack.Damage : 0;
            int special = kinSpecialAttack != null
                ? kinSpecialAttack.Damage
                : 0;

            SetKinProgress(kinHealthProgress, kinHealthValue, health,
                maximumHealth, $"{health}/{maximumHealth}");
            SetKinProgress(kinEnergyProgress, kinEnergyValue, energy,
                maximumEnergy, $"{energy}/{maximumEnergy}");
            SetKinProgress(kinAttackProgress, kinAttackValue, attack, 30,
                $"{attack} DANO");
            SetKinProgress(kinSpecialProgress, kinSpecialValue, special, 75,
                $"{special} DANO");

            if (kinLoadoutLabel != null)
            {
                string outfit = GetEquippedItemName(
                    HouseShopCategory.Clothes, "Samurai Vermelho");
                string accessory = GetEquippedItemName(
                    HouseShopCategory.Accessories, "Sem acessório");
                kinLoadoutLabel.text = $"{outfit}\n{accessory}";
            }
        }

        private static void SetKinProgress(Image progress, Text value,
            int current, int maximum, string label)
        {
            if (progress != null)
            {
                progress.fillAmount = maximum > 0
                    ? Mathf.Clamp01(current / (float)maximum)
                    : 0f;
            }

            if (value != null)
            {
                value.text = label;
            }
        }

        private static string GetEquippedItemName(HouseShopCategory category,
            string fallback)
        {
            string itemId = HouseShopSave.GetEquipped(category);
            if (string.IsNullOrEmpty(itemId))
            {
                return fallback;
            }

            foreach (HouseShopItem item in HouseShopCatalog.Get(category))
            {
                if (item.Id == itemId)
                {
                    return item.Name;
                }
            }

            return fallback;
        }

        private void RotateKinPreview()
        {
            if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;
            kinPreviewYaw = Mathf.Repeat(kinPreviewYaw + 180f, 360f);
            if (kinPreviewRoot == null)
            {
                return;
            }

            kinPreviewRoot.transform.rotation =
                Quaternion.Euler(0f, kinPreviewYaw, 0f);
            kinPreviewCamera?.Render();
        }

        private void RebuildKinPreview()
        {
            DestroyKinPreview();
            if (kinPreviewImage == null || kinHealth == null)
            {
                return;
            }

            Transform kin = kinHealth.transform;
            Transform sourceModel = kin.Find("GameplayModel");
            if (sourceModel == null)
            {
                return;
            }

            kinPreviewRoot = new GameObject("House Kin Preview");
            kinPreviewRoot.transform.SetParent(transform, false);
            kinPreviewRoot.transform.position = new Vector3(0f, -200f, 0f);
            kinPreviewRoot.transform.rotation =
                Quaternion.Euler(0f, kinPreviewYaw, 0f);

            ClonePreviewObject(sourceModel, kinPreviewRoot.transform,
                "Kin Preview Model");
            Transform sourceAccessory = FindLatestChild(kin,
                "Equipped Shop Accessory");
            if (sourceAccessory != null)
            {
                ClonePreviewObject(sourceAccessory, kinPreviewRoot.transform,
                    "Kin Preview Accessory");
            }

            SetLayerRecursively(kinPreviewRoot, KinPreviewLayer);
            foreach (Collider collider in
                kinPreviewRoot.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            foreach (Rigidbody body in
                kinPreviewRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            GameObject lightRoot = new("House Kin Preview Light");
            lightRoot.transform.SetParent(kinPreviewRoot.transform, false);
            lightRoot.transform.localPosition = new Vector3(-1.8f, 2.6f, -2f);
            Light previewLight = lightRoot.AddComponent<Light>();
            previewLight.type = LightType.Point;
            previewLight.range = 8f;
            previewLight.intensity = 2.2f;
            previewLight.color = new Color(1f, 0.78f, 0.52f);
            previewLight.cullingMask = 1 << KinPreviewLayer;

            GameObject cameraRoot = new("House Kin Preview Camera");
            cameraRoot.transform.SetParent(transform, false);
            Vector3 target = kinPreviewRoot.transform.position +
                new Vector3(0f, 0.08f, 0f);
            cameraRoot.transform.position = target +
                new Vector3(0f, 0.18f, -5f);
            cameraRoot.transform.rotation = Quaternion.LookRotation(
                target - cameraRoot.transform.position, Vector3.up);
            kinPreviewCamera = cameraRoot.AddComponent<Camera>();
            kinPreviewCamera.orthographic = true;
            kinPreviewCamera.orthographicSize = 1.7f;
            kinPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            kinPreviewCamera.backgroundColor =
                new Color(.10f, .19f, .17f, 1f);
            kinPreviewCamera.cullingMask = 1 << KinPreviewLayer;
            kinPreviewCamera.nearClipPlane = 0.05f;
            kinPreviewCamera.farClipPlane = 20f;

            kinPreviewTexture = new RenderTexture(512, 512, 16,
                RenderTextureFormat.ARGB32)
            {
                name = "House Kin Preview Texture"
            };
            kinPreviewTexture.Create();
            kinPreviewCamera.targetTexture = kinPreviewTexture;
            kinPreviewImage.texture = kinPreviewTexture;
            kinPreviewCamera.Render();
        }

        private static void ClonePreviewObject(Transform source,
            Transform parent, string previewName)
        {
            GameObject clone = Object.Instantiate(source.gameObject);
            clone.name = previewName;
            clone.transform.SetParent(parent, false);
            clone.transform.localPosition = source.localPosition;
            clone.transform.localRotation = source.localRotation;
            clone.transform.localScale = source.localScale;
            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = false;
            // This is a snapshot of the equipped gameplay model, including its gear.
            // Preview cameras must not run movement, rig setup or equipment effects.
            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
            Transform wardrobe = clone.transform.GetComponentsInChildren<Transform>(true).
                FirstOrDefault(t => t.name == "Equipped Kin Wardrobe");
            if (wardrobe != null)
                foreach (Renderer renderer in wardrobe.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            MeshFilter body = clone.GetComponentInChildren<MeshFilter>(true);
            SkinnedMeshRenderer bodyRig = clone.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (body != null && (bodyRig == null || !bodyRig.enabled))
            {
                MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
                if (bodyRenderer != null) bodyRenderer.enabled = true;
            }
        }

        private static Transform FindLatestChild(Transform parent,
            string childName)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void DestroyKinPreview()
        {
            if (kinPreviewImage != null)
            {
                kinPreviewImage.texture = null;
            }

            if (kinPreviewCamera != null)
            {
                kinPreviewCamera.enabled = false;
                kinPreviewCamera.targetTexture = null;
                Destroy(kinPreviewCamera.gameObject);
                kinPreviewCamera = null;
            }

            if (kinPreviewRoot != null)
            {
                Destroy(kinPreviewRoot);
                kinPreviewRoot = null;
            }

            if (kinPreviewTexture != null)
            {
                kinPreviewTexture.Release();
                Destroy(kinPreviewTexture);
                kinPreviewTexture = null;
            }
        }

        private void BuildShopPanel()
        {
            RectTransform shade = ui.Panel(
                "Computer Overlay",
                safeArea,
                RuntimeUiFactory.Veil,
                Vector2.zero
            );
            Stretch(shade);
            shopPanel = shade.gameObject;
            shopPanel.AddComponent<HudReveal>();

            RectTransform goldFrame = ui.Panel(
                "Computer Gold Frame",
                shade,
                new Color(.32f, .43f, .32f, 1),
                new Vector2(1570f, 980f)
            );
            RectTransform computer = ui.Panel(
                "Kin Computer",
                goldFrame,
                RuntimeUiFactory.Ink,
                new Vector2(1564f, 974f)
            );

            RuntimeUiFactory.Fit(goldFrame, new Vector2(72, 72));
            ui.Trim(computer, RuntimeUiFactory.Gold, true);
            Text title = ui.Label("Shop Title", "LOJA DO KIN",
                computer, 40, RuntimeUiFactory.Gold,
                new Vector2(720f, 58f), TextAnchor.MiddleCenter);
            ((RectTransform)title.transform).anchoredPosition =
                new Vector2(0f, 438f);

            shopCoinsLabel = ui.Label("Shop Coins", "MOEDAS  0", computer,
                24, RuntimeUiFactory.Gold, new Vector2(260f, 54f),
                TextAnchor.MiddleLeft);
            ((RectTransform)shopCoinsLabel.transform).anchoredPosition =
                new Vector2(-625f, 438f);

            Button close = ui.Button("Close Shop", "FECHAR", computer,
                RuntimeUiFactory.Jade, new Vector2(210f, 58f));
            ((RectTransform)close.transform).anchoredPosition =
                new Vector2(645f, 438f);
            close.onClick.AddListener(() => shopPanel.SetActive(false));

            string[] categories = { "ROUPAS", "ACESSÓRIOS", "EVOLUÇÕES" };
            for (int index = 0; index < categories.Length; index++)
            {
                HouseShopCategory category = (HouseShopCategory)index;
                Button categoryButton = ui.Button(
                    $"Shop {categories[index]}",
                    categories[index],
                    computer,
                    RuntimeUiFactory.Jade,
                    new Vector2(285f, 62f)
                );
                ((RectTransform)categoryButton.transform).anchoredPosition =
                    new Vector2((index - 1) * 315f, 370f);
                categoryButtons[index] = categoryButton;
                categoryButton.onClick.AddListener(
                    () => SelectShopCategory(category));
            }

            catalogFrame = ui.Panel(
                "Catalog Frame",
                computer,
                new Color(.10f, .2f, .17f, 1),
                new Vector2(1450f, 650f)
            );
            catalogFrame.anchoredPosition = new Vector2(0f, -30f);

            catalogArtworkRect = ui.Rect("Catalog Artwork", catalogFrame);
            catalogArtwork =
                catalogArtworkRect.gameObject.AddComponent<RawImage>();
            catalogArtwork.color = Color.white;
            catalogArtwork.raycastTarget = false;

            catalogOverlay = ui.Rect("Catalog Item Buttons", catalogArtworkRect);
            Stretch(catalogOverlay);

            RectTransform selectedPanel = ui.Panel(
                "Selected Item",
                computer,
                RuntimeUiFactory.Paper,
                new Vector2(1110f, 104f)
            );
            selectedPanel.anchoredPosition = new Vector2(-157f, -421f);

            selectedName = ui.Label("Selected Name", "ESCOLHA UM ITEM",
                selectedPanel, 25, RuntimeUiFactory.Ink,
                new Vector2(390f, 42f), TextAnchor.MiddleLeft);
            ((RectTransform)selectedName.transform).anchoredPosition =
                new Vector2(-335f, 22f);
            selectedDescription = ui.Label("Selected Description", "",
                selectedPanel, 18, RuntimeUiFactory.Ink,
                new Vector2(670f, 52f), TextAnchor.MiddleLeft);
            ((RectTransform)selectedDescription.transform).anchoredPosition =
                new Vector2(120f, -18f);

            shopActionButton = ui.Button(
                "Shop Action",
                "SELECIONE",
                computer,
                RuntimeUiFactory.Jade,
                new Vector2(300f, 104f)
            );
            ((RectTransform)shopActionButton.transform).anchoredPosition =
                new Vector2(570f, -421f);
            shopActionButton.onClick.AddListener(UseSelectedItem);
            shopActionLabel = shopActionButton.GetComponentInChildren<Text>();

            shopStatus = ui.Label("Shop Status", "", computer, 18,
                RuntimeUiFactory.Gold, new Vector2(720f, 32f),
                TextAnchor.MiddleCenter);
            ((RectTransform)shopStatus.transform).anchoredPosition =
                new Vector2(0f, 319f);

            shopPanel.SetActive(false);
        }

        private void ShowShop()
        {
            if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
            shopStatus.text =
                "Escolha um item. Compras e equipamentos são salvos automaticamente.";
            SelectShopCategory(HouseShopCategory.Clothes);
            UpdateCoinLabels();
        }

        private void SelectShopCategory(HouseShopCategory category)
        {
            selectedCategory = category;
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                Button tab = categoryButtons[i]; bool active = i == (int)category;
                tab.GetComponent<Image>().color = active ? RuntimeUiFactory.Gold : RuntimeUiFactory.Jade;
                tab.GetComponentInChildren<Text>().color = active ? RuntimeUiFactory.Ink : RuntimeUiFactory.Paper;
                tab.GetComponent<HudMotion>().SetSelected(active);
            }
            IReadOnlyList<HouseShopItem> items = HouseShopCatalog.Get(category);
            selectedItem = items.Count > 0 ? items[0] : null;
            RefreshCatalog();
            RefreshSelectedItem();
        }

        private void RefreshCatalog()
        {
            catalogButtons.Clear();
            for (int index = catalogOverlay.childCount - 1; index >= 0; index--)
            {
                Destroy(catalogOverlay.GetChild(index).gameObject);
            }

            Texture2D texture = selectedCategory switch
            {
                HouseShopCategory.Clothes => clothesArtwork,
                HouseShopCategory.Accessories => accessoriesArtwork,
                _ => null
            };
            catalogArtwork.texture = texture;
            catalogArtwork.enabled = texture != null;
            LayoutCatalogArtwork(texture);

            IReadOnlyList<HouseShopItem> items =
                HouseShopCatalog.Get(selectedCategory);
            foreach (HouseShopItem item in items)
            {
                CreateCatalogItemButton(item, texture == null);
            }
        }

        private void LayoutCatalogArtwork(Texture2D texture)
        {
            Canvas.ForceUpdateCanvases();
            float frameWidth = Mathf.Max(100f, catalogFrame.rect.width - 18f);
            float frameHeight = Mathf.Max(100f, catalogFrame.rect.height - 18f);

            if (texture == null)
            {
                catalogArtworkRect.sizeDelta =
                    new Vector2(frameWidth, frameHeight);
                return;
            }

            float imageAspect = texture.width / (float)texture.height;
            float frameAspect = frameWidth / frameHeight;
            catalogArtworkRect.sizeDelta = imageAspect > frameAspect
                ? new Vector2(frameWidth, frameWidth / imageAspect)
                : new Vector2(frameHeight * imageAspect, frameHeight);
        }

        private void CreateCatalogItemButton(
            HouseShopItem item,
            bool drawFullCard)
        {
            Rect region = item.ArtworkRegion;
            RectTransform rect = ui.Rect(item.Name, catalogOverlay);
            rect.anchorMin = new Vector2(region.xMin, region.yMin);
            rect.anchorMax = new Vector2(region.xMax, region.yMax);
            rect.offsetMin = new Vector2(5f, 5f);
            rect.offsetMax = new Vector2(-5f, -5f);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GameUiArt.Rounded; image.type = Image.Type.Sliced;
            image.color = drawFullCard ? RuntimeUiFactory.Paper : new Color(.85f, .72f, .45f, .015f);
            Button button = rect.gameObject.AddComponent<Button>(); RuntimeUiFactory.ConfigureButton(button);
            button.targetGraphic = image; rect.gameObject.AddComponent<HudMotion>();
            ui.Trim(rect, RuntimeUiFactory.Gold);
            rect.Find("Fine Gold Contour").gameObject.SetActive(false);
            catalogButtons[item] = button;
            button.onClick.AddListener(() => SelectItem(item));

            if (drawFullCard)
            {
                Text name = ui.Label("Upgrade Name", item.Name.ToUpperInvariant(),
                    rect, 27, RuntimeUiFactory.Jade,
                    new Vector2(350f, 58f), TextAnchor.MiddleCenter);
                ((RectTransform)name.transform).anchorMin =
                    ((RectTransform)name.transform).anchorMax =
                    new Vector2(0.5f, 0.72f);

                Text description = ui.Label("Upgrade Description",
                    item.Description, rect, 19, RuntimeUiFactory.Ink,
                    new Vector2(350f, 105f), TextAnchor.MiddleCenter);
                ((RectTransform)description.transform).anchorMin =
                    ((RectTransform)description.transform).anchorMax =
                    new Vector2(0.5f, 0.43f);
            }

            RectTransform badge = ui.Panel(
                "Item State",
                rect,
                HouseShopSave.IsEquipped(item)
                    ? RuntimeUiFactory.Jade
                    : RuntimeUiFactory.Ink,
                new Vector2(drawFullCard ? 190f : 165f, 42f)
            );
            badge.GetComponent<Image>().raycastTarget = false;
            badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
            badge.pivot = new Vector2(0.5f, 1f);
            badge.anchoredPosition = new Vector2(0f, -8f);
            ui.Label("State Label", GetItemState(item), badge, 17,
                Color.white, badge.sizeDelta - new Vector2(8f, 6f),
                TextAnchor.MiddleCenter);
        }

        private void SelectItem(HouseShopItem item)
        {
            if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;
            selectedItem = item;
            RefreshSelectedItem();
            shopStatus.text = HouseShopSave.IsOwned(item)
                ? item.IsCosmetic
                    ? "Item disponível no inventário do Kin."
                    : "Evolução já aplicada permanentemente."
                : $"Preço: {item.Price} moedas.";
        }

        private void RefreshSelectedItem()
        {
            if (selectedItem == null)
            {
                selectedName.text = "ESCOLHA UM ITEM";
                selectedDescription.text = string.Empty;
                shopActionLabel.text = "SELECIONE";
                shopActionButton.interactable = false;
                return;
            }

            foreach (var entry in catalogButtons)
            {
                bool chosen = entry.Key == selectedItem;
                entry.Value.transform.Find("Fine Gold Contour").gameObject.SetActive(chosen);
                entry.Value.GetComponent<HudMotion>().SetSelected(chosen);
            }
            selectedName.text = selectedItem.Name.ToUpperInvariant();
            selectedDescription.text = selectedItem.Description;
            bool owned = HouseShopSave.IsOwned(selectedItem);
            bool equipped = HouseShopSave.IsEquipped(selectedItem);

            if (!owned)
            {
                shopActionLabel.text = $"COMPRAR\n{selectedItem.Price} MOEDAS";
                shopActionButton.interactable = true;
            }
            else if (selectedItem.IsCosmetic && !equipped)
            {
                shopActionLabel.text = "EQUIPAR";
                shopActionButton.interactable = true;
            }
            else
            {
                shopActionLabel.text = selectedItem.IsCosmetic
                    ? "EQUIPADO"
                    : "COMPRADO";
                shopActionButton.interactable = false;
            }
        }

        private void UseSelectedItem()
        {
            if (KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame) return;
            if (selectedItem == null)
            {
                return;
            }

            bool changed;
            string result;
            if (HouseShopSave.IsOwned(selectedItem))
            {
                changed = HouseShopSave.TryEquip(selectedItem, out result);
            }
            else
            {
                changed = HouseShopSave.TryPurchase(
                    selectedItem, wallet, out result);
            }

            shopStatus.text = result;
            if (changed)
            {
                ApplyPurchasedBenefits();
                RebuildKinPreview();
                RefreshKinProfile();
            }
            UpdateCoinLabels();
            RefreshCatalog();
            RefreshSelectedItem();
        }

        private static string GetItemState(HouseShopItem item)
        {
            if (HouseShopSave.IsEquipped(item)) return "EQUIPADO";
            if (HouseShopSave.IsOwned(item))
                return item.IsCosmetic ? "NO INVENTÁRIO" : "COMPRADO";
            return $"{item.Price} MOEDAS";
        }

        private void UpdateCoinLabels()
        {
            string value = $"MOEDAS  {(wallet != null ? wallet.Coins : 0)}";
            if (coinsLabel != null) coinsLabel.text = value;
            if (shopCoinsLabel != null) shopCoinsLabel.text = value;
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;
            min.x /= Mathf.Max(1f, Screen.width);
            min.y /= Mathf.Max(1f, Screen.height);
            max.x /= Mathf.Max(1f, Screen.width);
            max.y /= Mathf.Max(1f, Screen.height);
            safeArea.anchorMin = min;
            safeArea.anchorMax = max;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
