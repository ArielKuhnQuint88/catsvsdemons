using System.Collections.Generic;
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
                "COMPUTADOR: LOJA  •  PERFIL: EQUIPAMENTO DO KIN  •  PRÓXIMA FASE");
            UpdateCoinLabels();
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
                "COMPUTADOR: LOJA  •  PERFIL: EQUIPAMENTO DO KIN  •  CONTINUAR BATALHA");
            UpdateCoinLabels();
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
                new Color(0.006f, 0.014f, 0.04f, 1f),
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
                new Color(0.025f, 0.37f, 0.72f, 0.88f),
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
                new Color(0.94f, 0.55f, 0.10f, 0.98f),
                new Vector2(820f, 132f)
            );
            headerBorder.anchorMin = headerBorder.anchorMax =
                new Vector2(0.5f, 1f);
            headerBorder.pivot = new Vector2(0.5f, 1f);
            headerBorder.anchoredPosition = new Vector2(0f, -18f);

            RectTransform header = ui.Panel(
                "House Header",
                headerBorder,
                new Color(0.17f, 0.045f, 0.025f, 0.97f),
                new Vector2(806f, 118f)
            );

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
                new Color(0.12f, 0.035f, 0.02f, 0.94f),
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
                "COMPUTADOR: LOJA  •  PERFIL: EQUIPAMENTO DO KIN  •  PRÓXIMA FASE",
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
                new Color(0.63f, 0.075f, 0.035f, 0.97f),
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
                new Color(0.94f, 0.55f, 0.10f, 0.98f),
                new Vector2(462f, 704f)
            );
            border.anchorMin = border.anchorMax = new Vector2(0f, 0f);
            border.pivot = new Vector2(0f, 0f);
            border.anchoredPosition = new Vector2(24f, 84f);

            RectTransform panel = ui.Panel(
                "Kin Profile",
                border,
                new Color(0.055f, 0.018f, 0.025f, 0.96f),
                new Vector2(448f, 690f)
            );

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
                new Color(0.010f, 0.028f, 0.075f, 0.98f),
                new Vector2(370f, 266f)
            );
            portraitFrame.anchoredPosition = new Vector2(0f, 96f);
            RectTransform portrait = ui.Rect("Kin Preview", portraitFrame);
            portrait.sizeDelta = new Vector2(354f, 250f);
            kinPreviewImage = portrait.gameObject.AddComponent<RawImage>();
            kinPreviewImage.color = Color.white;
            kinPreviewImage.raycastTarget = false;

            kinHealthProgress = CreateKinProgressBar(panel, "VIDA", -80f,
                new Color(0.92f, 0.18f, 0.12f), out kinHealthValue);
            kinEnergyProgress = CreateKinProgressBar(panel, "ENERGIA", -150f,
                new Color(0.18f, 0.75f, 0.34f), out kinEnergyValue);
            kinAttackProgress = CreateKinProgressBar(panel, "ATAQUE", -220f,
                new Color(0.98f, 0.60f, 0.10f), out kinAttackValue);
            kinSpecialProgress = CreateKinProgressBar(panel, "GOLPE", -290f,
                new Color(0.56f, 0.34f, 0.95f), out kinSpecialValue);
        }

        private Image CreateKinProgressBar(Transform parent, string label,
            float verticalPosition, Color color, out Text value)
        {
            Text labelText = ui.Label($"{label} Label", label, parent, 18,
                Color.white, new Vector2(160f, 24f), TextAnchor.MiddleLeft);
            ((RectTransform)labelText.transform).anchoredPosition =
                new Vector2(-166f, verticalPosition);

            value = ui.Label($"{label} Value", "", parent, 18,
                RuntimeUiFactory.Gold, new Vector2(160f, 24f),
                TextAnchor.MiddleRight);
            ((RectTransform)value.transform).anchoredPosition =
                new Vector2(166f, verticalPosition);

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
                new Color(0.010f, 0.028f, 0.075f, 1f);
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
                new Color(0.004f, 0.009f, 0.025f, 0.96f),
                Vector2.zero
            );
            Stretch(shade);
            shopPanel = shade.gameObject;

            RectTransform goldFrame = ui.Panel(
                "Computer Gold Frame",
                shade,
                new Color(0.88f, 0.48f, 0.08f, 1f),
                new Vector2(1580f, 990f)
            );
            RectTransform computer = ui.Panel(
                "Kin Computer",
                goldFrame,
                new Color(0.075f, 0.024f, 0.018f, 1f),
                new Vector2(1564f, 974f)
            );

            Text title = ui.Label("Shop Title", "COMPUTADOR DO KIN",
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
                new Color(0.56f, 0.07f, 0.035f), new Vector2(210f, 58f));
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
                    index == 2
                        ? new Color(0.45f, 0.15f, 0.55f)
                        : new Color(0.45f, 0.075f, 0.035f),
                    new Vector2(285f, 62f)
                );
                ((RectTransform)categoryButton.transform).anchoredPosition =
                    new Vector2((index - 1) * 315f, 370f);
                categoryButton.onClick.AddListener(
                    () => SelectShopCategory(category));
            }

            catalogFrame = ui.Panel(
                "Catalog Frame",
                computer,
                new Color(0.015f, 0.012f, 0.015f, 1f),
                new Vector2(1450f, 690f)
            );
            catalogFrame.anchoredPosition = new Vector2(0f, -5f);

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
                new Color(0.88f, 0.77f, 0.57f, 0.98f),
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
                new Color(0.12f, 0.42f, 0.18f, 1f),
                new Vector2(300f, 104f)
            );
            ((RectTransform)shopActionButton.transform).anchoredPosition =
                new Vector2(570f, -421f);
            shopActionButton.onClick.AddListener(UseSelectedItem);
            shopActionLabel = shopActionButton.GetComponentInChildren<Text>();

            shopStatus = ui.Label("Shop Status", "", computer, 18,
                new Color(0.95f, 0.73f, 0.25f), new Vector2(720f, 32f),
                TextAnchor.MiddleCenter);
            ((RectTransform)shopStatus.transform).anchoredPosition =
                new Vector2(0f, 329f);

            shopPanel.SetActive(false);
        }

        private void ShowShop()
        {
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
            IReadOnlyList<HouseShopItem> items = HouseShopCatalog.Get(category);
            selectedItem = items.Count > 0 ? items[0] : null;
            RefreshCatalog();
            RefreshSelectedItem();
        }

        private void RefreshCatalog()
        {
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
            image.sprite = null;
            image.color = drawFullCard
                ? new Color(0.88f, 0.76f, 0.55f, 0.98f)
                : new Color(1f, 0.58f, 0.10f, 0.015f);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = drawFullCard
                ? new Color(1f, 0.86f, 0.59f, 1f)
                : new Color(1f, 0.58f, 0.10f, 0.22f);
            colors.pressedColor = new Color(0.95f, 0.48f, 0.08f, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => SelectItem(item));

            if (drawFullCard)
            {
                Text name = ui.Label("Upgrade Name", item.Name.ToUpperInvariant(),
                    rect, 27, new Color(0.43f, 0.055f, 0.025f),
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
                    ? new Color(0.08f, 0.46f, 0.18f, 0.96f)
                    : new Color(0.34f, 0.045f, 0.025f, 0.94f),
                new Vector2(drawFullCard ? 190f : 165f, 42f)
            );
            badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
            badge.pivot = new Vector2(0.5f, 1f);
            badge.anchoredPosition = new Vector2(0f, -8f);
            ui.Label("State Label", GetItemState(item), badge, 17,
                Color.white, badge.sizeDelta - new Vector2(8f, 6f),
                TextAnchor.MiddleCenter);
        }

        private void SelectItem(HouseShopItem item)
        {
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
