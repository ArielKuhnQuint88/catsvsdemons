using System.Collections.Generic;
using CatsVsDemons.CameraSystem;
using CatsVsDemons.Economy;
using CatsVsDemons.UI;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.House
{
    public sealed class HouseIntermissionController : MonoBehaviour
    {
        private readonly Dictionary<GameObject, bool> battleStates = new();

        private EnemyWaveSpawner waves;
        private Wallet wallet;
        private ResponsiveCanvasHud battleHud;
        private CameraModeController cameraController;
        private Camera gameCamera;
        private GameObject interiorRoot;
        private GameObject interfaceRoot;
        private GameObject shopPanel;
        private RectTransform safeArea;
        private Text phaseTitle;
        private Text phaseMessage;
        private Text coinsLabel;
        private Text shopDescription;
        private Rect lastSafeArea;
        private bool isOpen;

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
            interiorRoot = HouseInteriorBuilder.Build(parent);
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
            coinsLabel.text = $"MOEDAS  {(wallet != null ? wallet.Coins : 0)}";
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
        }

        private void FindSceneReferences()
        {
            wallet ??= Object.FindFirstObjectByType<Wallet>();
            battleHud ??= Object.FindFirstObjectByType<ResponsiveCanvasHud>();
            cameraController ??=
                Object.FindFirstObjectByType<CameraModeController>();
            gameCamera ??= Camera.main;
        }

        private void EnterIntermission(int completedPhase, int totalPhases)
        {
            if (isOpen)
            {
                return;
            }

            FindSceneReferences();
            isOpen = true;
            phaseTitle.text = $"FASE {completedPhase} CONCLUÍDA";
            phaseMessage.text =
                $"A casa está segura. Prepare Kin para a fase " +
                $"{completedPhase + 1} de {totalPhases}.";
            coinsLabel.text = $"MOEDAS  {(wallet != null ? wallet.Coins : 0)}";

            CaptureAndHideBattlefield();
            ConfigureHouseCamera();
            interiorRoot.SetActive(true);
            shopPanel.SetActive(false);
            interfaceRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        private void ContinueToNextPhase()
        {
            if (!isOpen)
            {
                return;
            }

            shopPanel.SetActive(false);
            interfaceRoot.SetActive(false);
            interiorRoot.SetActive(false);
            RestoreBattlefield();
            RestoreCamera();
            isOpen = false;
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
            RuntimeUiFactory ui = new();
            Canvas canvas = ui.CreateCanvas(transform);
            canvas.gameObject.name = "House Intermission UI";
            canvas.sortingOrder = 650;
            interfaceRoot = canvas.gameObject;

            safeArea = ui.Rect("Safe Area", canvas.transform);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            RectTransform header = ui.Panel(
                "House Header",
                safeArea,
                new Color(0.92f, 0.78f, 0.55f, 0.97f),
                new Vector2(900f, 150f)
            );
            header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -24f);

            phaseTitle = ui.Label("Phase Title", "FASE CONCLUÍDA", header,
                38, RuntimeUiFactory.Ink, new Vector2(820f, 58f),
                TextAnchor.MiddleCenter);
            ((RectTransform)phaseTitle.transform).anchoredPosition =
                new Vector2(0f, 32f);
            phaseMessage = ui.Label("Phase Message", "", header, 21,
                RuntimeUiFactory.Ink, new Vector2(820f, 48f),
                TextAnchor.MiddleCenter);
            ((RectTransform)phaseMessage.transform).anchoredPosition =
                new Vector2(0f, -22f);

            coinsLabel = ui.Label("Coins", "MOEDAS  0", safeArea, 28,
                RuntimeUiFactory.Gold, new Vector2(300f, 64f),
                TextAnchor.MiddleLeft);
            RectTransform coinsRect = (RectTransform)coinsLabel.transform;
            coinsRect.anchorMin = coinsRect.anchorMax = new Vector2(0f, 1f);
            coinsRect.pivot = new Vector2(0f, 1f);
            coinsRect.anchoredPosition = new Vector2(28f, -28f);

            Text instruction = ui.Label(
                "House Instruction",
                "DESCANSE  •  PERSONALIZE  •  EVOLUA",
                safeArea,
                21,
                Color.white,
                new Vector2(660f, 48f),
                TextAnchor.MiddleCenter
            );
            RectTransform instructionRect =
                (RectTransform)instruction.transform;
            instructionRect.anchorMin = instructionRect.anchorMax =
                new Vector2(0.5f, 0f);
            instructionRect.pivot = new Vector2(0.5f, 0f);
            instructionRect.anchoredPosition = new Vector2(0f, 24f);

            Button computer = ui.Button(
                "Computer Shop",
                "COMPUTADOR\nABRIR LOJA",
                safeArea,
                new Color(0.04f, 0.34f, 0.68f, 0.96f),
                new Vector2(310f, 96f)
            );
            RectTransform computerRect = (RectTransform)computer.transform;
            computerRect.anchorMin = computerRect.anchorMax =
                new Vector2(0f, 0f);
            computerRect.pivot = new Vector2(0f, 0f);
            computerRect.anchoredPosition = new Vector2(28f, 28f);
            computer.onClick.AddListener(ShowShop);

            Button nextPhase = ui.Button(
                "Next Phase",
                "PRÓXIMA FASE",
                safeArea,
                new Color(0.63f, 0.075f, 0.035f, 0.97f),
                new Vector2(310f, 96f)
            );
            RectTransform nextRect = (RectTransform)nextPhase.transform;
            nextRect.anchorMin = nextRect.anchorMax = new Vector2(1f, 0f);
            nextRect.pivot = new Vector2(1f, 0f);
            nextRect.anchoredPosition = new Vector2(-28f, 28f);
            nextPhase.onClick.AddListener(ContinueToNextPhase);

            BuildShopPanel(ui);
            ApplySafeArea();
            interfaceRoot.SetActive(false);
        }

        private void BuildShopPanel(RuntimeUiFactory ui)
        {
            RectTransform shade = ui.Panel(
                "Computer Overlay",
                safeArea,
                new Color(0.005f, 0.012f, 0.035f, 0.88f),
                Vector2.zero
            );
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = Vector2.one;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            shopPanel = shade.gameObject;

            RectTransform computer = ui.Panel(
                "Kin Computer",
                shade,
                new Color(0.055f, 0.11f, 0.17f, 0.99f),
                new Vector2(1080f, 650f)
            );

            Text title = ui.Label("Shop Title", "COMPUTADOR DO KIN",
                computer, 42, RuntimeUiFactory.Gold,
                new Vector2(920f, 70f), TextAnchor.MiddleCenter);
            ((RectTransform)title.transform).anchoredPosition =
                new Vector2(0f, 245f);

            Text subtitle = ui.Label(
                "Shop Subtitle",
                "LOJA  •  INVENTÁRIO  •  EVOLUÇÕES",
                computer,
                20,
                new Color(0.52f, 0.84f, 1f),
                new Vector2(820f, 42f),
                TextAnchor.MiddleCenter
            );
            ((RectTransform)subtitle.transform).anchoredPosition =
                new Vector2(0f, 193f);

            string[] categories = { "ROUPAS", "ACESSÓRIOS", "EVOLUÇÕES" };
            for (int index = 0; index < categories.Length; index++)
            {
                int captured = index;
                Button category = ui.Button(
                    $"Shop {categories[index]}",
                    categories[index],
                    computer,
                    index == 2
                        ? new Color(0.48f, 0.19f, 0.62f)
                        : new Color(0.08f, 0.34f, 0.50f),
                    new Vector2(270f, 72f)
                );
                ((RectTransform)category.transform).anchoredPosition =
                    new Vector2((index - 1) * 300f, 112f);
                category.onClick.AddListener(() => SelectShopCategory(captured));
            }

            RectTransform descriptionPanel = ui.Panel(
                "Shop Description Panel",
                computer,
                new Color(0.86f, 0.75f, 0.55f, 0.97f),
                new Vector2(880f, 220f)
            );
            descriptionPanel.anchoredPosition = new Vector2(0f, -55f);
            shopDescription = ui.Label(
                "Shop Description",
                "Escolha uma categoria para preparar Kin.",
                descriptionPanel,
                25,
                RuntimeUiFactory.Ink,
                new Vector2(810f, 170f),
                TextAnchor.MiddleCenter
            );

            Button close = ui.Button("Close Shop", "FECHAR", computer,
                new Color(0.56f, 0.07f, 0.035f), new Vector2(260f, 72f));
            ((RectTransform)close.transform).anchoredPosition =
                new Vector2(0f, -266f);
            close.onClick.AddListener(() => shopPanel.SetActive(false));
            shopPanel.SetActive(false);
        }

        private void ShowShop()
        {
            shopDescription.text =
                "Escolha ROUPAS, ACESSÓRIOS ou EVOLUÇÕES. " +
                "As compras usarão as moedas conquistadas nas batalhas.";
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
        }

        private void SelectShopCategory(int category)
        {
            shopDescription.text = category switch
            {
                0 => "ROUPAS\nSamurai Vermelho, Ninja da Meia-Noite, " +
                    "Guardião do Bonsai e novos trajes entrarão aqui.",
                1 => "ACESSÓRIOS\nFaixas, máscaras, coleiras, amuletos e " +
                    "efeitos poderão ser comprados e equipados aqui.",
                _ => "EVOLUÇÕES\nMelhore a vida, a energia e o golpe de Kin " +
                    "antes de enfrentar a próxima fase."
            };
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
    }
}
