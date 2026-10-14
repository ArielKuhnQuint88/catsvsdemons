using System.Collections.Generic;
using CatsVsDemons.CameraSystem;
using CatsVsDemons.Defense;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using CatsVsDemons.Player;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    [DefaultExecutionOrder(250)]
    public sealed class BattleMinimap : MonoBehaviour
    {
        private readonly List<EnemyHealth> enemies = new(128);
        private readonly List<DefenseHealth> towers = new(32);
        private readonly List<MinimapMarker> markers = new(160);
        private readonly List<Vector3[]> fallbackRoads = new();
        private CameraModeController cameraMode;
        private PhaseEnvironmentController environment;
        private KinHealth kin;
        private HouseHealth house;
        private BattleMinimapGraphic terrain, actors;
        private Button switchView, zoomOut, zoomIn;
        private Text modeText, heading, zoomText;
        private Image modeIcon;
        private RectTransform mapSurface;
        private IReadOnlyList<Vector3[]> worldRoads;
        private MinimapProjection overview, displayedProjection;
        private bool hasProjection;
        private int zoomStep;
        private int revision = -1;
        private float nextRefresh, nextSearch;
        private bool interactionAllowed = true;
        public int VisibleDemons { get; private set; }
        public int VisibleTowers { get; private set; }
        public int TowersUnderAttack { get; private set; }
        public float Zoom => 1f + zoomStep * .5f;

        internal void Initialize(RuntimeUiFactory ui, RectTransform safeArea)
        {
            zoomStep = Mathf.Clamp(PlayerPrefs.GetInt("MinimapZoomStep", 2), 0, 4);
            RectTransform card = ui.Surface("Battle Minimap", safeArea, RuntimeUiFactory.Ink, new Vector2(440, 492));
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(1, 1); card.anchoredPosition = new Vector2(-30, -188);
            heading = ui.Label("Map Heading", "MAPA DO JARDIM", card, 20, RuntimeUiFactory.Gold, new Vector2(244, 44), TextAnchor.MiddleLeft);
            heading.rectTransform.anchoredPosition = new Vector2(-72, 211);
            zoomOut = ui.Button("Map Zoom Out", "−", card, RuntimeUiFactory.Jade, new Vector2(44, 44));
            ((RectTransform)zoomOut.transform).anchoredPosition = new Vector2(76, 211);
            zoomText = ui.Label("Map Zoom Level", "2×", card, 18, RuntimeUiFactory.Paper, new Vector2(48, 40), TextAnchor.MiddleCenter);
            zoomText.rectTransform.anchoredPosition = new Vector2(130, 211);
            zoomIn = ui.Button("Map Zoom In", "+", card, RuntimeUiFactory.Jade, new Vector2(44, 44));
            ((RectTransform)zoomIn.transform).anchoredPosition = new Vector2(184, 211);
            zoomOut.onClick.AddListener(() => ChangeZoom(-1));
            zoomIn.onClick.AddListener(() => ChangeZoom(1));
            RectTransform map = ui.Panel("Map Surface", card, new Color(.13f, .24f, .19f), new Vector2(400, 304));
            mapSurface = map;
            map.anchoredPosition = new Vector2(0, 31); map.gameObject.AddComponent<RectMask2D>();
            RectTransform roads = ui.GraphicRect("Map Roads", map); RuntimeUiFactory.Stretch(roads, Vector2.zero);
            terrain = roads.gameObject.AddComponent<BattleMinimapGraphic>(); terrain.Terrain = true; terrain.raycastTarget = false;
            RectTransform dots = ui.GraphicRect("Map Actors", map); RuntimeUiFactory.Stretch(dots, Vector2.zero);
            actors = dots.gameObject.AddComponent<BattleMinimapGraphic>(); actors.raycastTarget = false;
            Text legend = ui.Label("Map Legend", "<color=#F7F0DA>▲ Kin</color>    <color=#E67862>● Demônios</color>    <color=#D9B873>◆ Torres</color>", card, 16,
                RuntimeUiFactory.Paper, new Vector2(400, 30), TextAnchor.MiddleCenter);
            legend.rectTransform.anchoredPosition = new Vector2(0, -146);
            switchView = ui.Button("Switch Camera View", "PRIMEIRA PESSOA", card, RuntimeUiFactory.Jade, new Vector2(400, 62));
            ((RectTransform)switchView.transform).anchoredPosition = new Vector2(0, -198);
            modeText = switchView.GetComponentInChildren<Text>(); modeText.fontSize = modeText.resizeTextMaxSize = 20;
            modeText.rectTransform.sizeDelta = new Vector2(314, 52); modeText.rectTransform.anchoredPosition = new Vector2(20, 0);
            modeIcon = ui.Icon("Camera Mode Symbol", UiIcon.FirstPerson, switchView.transform, new Vector2(29, 29), RuntimeUiFactory.Gold);
            modeIcon.rectTransform.anchoredPosition = new Vector2(-164, 0);
            switchView.onClick.AddListener(() => ToggleView());
            FindSystems(); RefreshMap(); RefreshButton();
        }
        public void SetInteractionAllowed(bool value) => interactionAllowed = value;
        private bool CanUseMap => interactionAllowed && !KinGuideOverlay.IsOpen && !KinGuideOverlay.ConsumedInputThisFrame && Time.timeScale > 0;
        public bool ChangeZoom(int steps)
        {
            if (!CanUseMap) return false;
            int next = Mathf.Clamp(zoomStep + steps, 0, 4);
            if (next == zoomStep) return false;
            zoomStep = next;
            PlayerPrefs.SetInt("MinimapZoomStep", zoomStep); PlayerPrefs.Save();
            RefreshMap(); RefreshButton();
            return true;
        }
        public bool ToggleView()
        {
            if (!CanUseMap) return false;
            FindSystems();
            bool switched = cameraMode != null && cameraMode.ToggleView();
            if (switched) { switchView.GetComponent<HudMotion>().Punch(); RefreshButton(); }
            return switched;
        }
        private void FindSystems()
        {
            if (Camera.main != null && cameraMode == null) cameraMode = Camera.main.GetComponent<CameraModeController>();
            if (environment == null) environment = Object.FindFirstObjectByType<PhaseEnvironmentController>();
            if (kin == null) kin = Object.FindFirstObjectByType<KinHealth>();
            if (house == null) house = Object.FindFirstObjectByType<HouseHealth>();
        }
        private void LateUpdate()
        {
            if (terrain == null || !terrain.gameObject.activeInHierarchy) return;
            if (Time.unscaledTime >= nextSearch) { nextSearch = Time.unscaledTime + .5f; FindSystems(); }
            RefreshButton();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .05f; RefreshMap();
        }
        private void RefreshButton()
        {
            bool first = cameraMode != null && cameraMode.IsFirstPerson;
            modeText.text = Application.isMobilePlatform ? (first ? "ISOMÉTRICO" : "PRIMEIRA PESSOA") : (first ? "ISOMÉTRICO  ·  V" : "PRIMEIRA PESSOA  ·  V");
            modeIcon.sprite = GameUiArt.Symbol(first ? UiIcon.Isometric : UiIcon.FirstPerson);
            switchView.interactable = interactionAllowed && cameraMode != null && cameraMode.CanSwitchView;
            zoomText.text = Zoom.ToString("0.#") + "×";
            zoomOut.interactable = CanUseMap && zoomStep > 0;
            zoomIn.interactable = CanUseMap && zoomStep < 4;
        }
        private void RefreshMap()
        {
            int current = environment != null ? environment.RouteRevision : 0;
            if (revision != current)
            {
                revision = current;
                worldRoads = environment != null ? environment.WorldRoutes : ReadSceneRoads();
                Vector3 center = house != null ? house.transform.position : Vector3.zero;
                overview = MinimapProjection.FromRoutes(worldRoads, center);
                hasProjection = false;
            }
            Vector3 focus = house != null ? house.transform.position : kin != null ? kin.transform.position : Vector3.zero;
            MinimapProjection projection = overview.WithZoom(Zoom, focus);
            if (Zoom > 1 && kin != null) projection.KeepVisible(kin.transform.position, mapSurface.rect.size);
            if (!hasProjection || (projection.Center - displayedProjection.Center).sqrMagnitude > .0001f ||
                (projection.WorldSize - displayedProjection.WorldSize).sqrMagnitude > .0001f)
            {
                displayedProjection = projection; hasProjection = true;
                terrain.SetMap(projection, worldRoads); actors.SetMap(projection, null);
            }
            markers.Clear(); EnemyRegistry.CopyActiveTo(enemies); DefenseRegistry.CopyActiveTo(towers);
            VisibleDemons = enemies.Count; VisibleTowers = towers.Count; TowersUnderAttack = 0;
            foreach (EnemyHealth enemy in enemies) markers.Add(new MinimapMarker { Position = enemy.transform.position, Kind = MinimapKind.Demon });
            foreach (DefenseHealth tower in towers)
            {
                bool alarm = Time.time - tower.LastDamageTime < 2.5f;
                if (alarm) TowersUnderAttack++;
                Color tint = tower.GetComponent<PortalTransport>() != null ? new Color(.46f, .79f, .93f) :
                    tower.GetComponent<BonsaiHealing>() != null ? new Color(.57f, .85f, .53f) : RuntimeUiFactory.Gold;
                markers.Add(new MinimapMarker { Position = tower.transform.position, Kind = MinimapKind.Tower, UnderAttack = alarm, Color = tint });
            }
            if (house != null) markers.Add(new MinimapMarker { Position = house.transform.position, Kind = MinimapKind.Home });
            if (kin != null) markers.Add(new MinimapMarker { Position = kin.transform.position, Forward = kin.transform.forward, Kind = MinimapKind.Kin });
            heading.text = TowersUnderAttack > 0 ? "TORRES SOB ATAQUE  ·  " + TowersUnderAttack : "MAPA DO JARDIM";
            heading.color = TowersUnderAttack > 0 ? new Color(1, .58f, .42f) : RuntimeUiFactory.Gold;
            actors.SetActors(markers, Time.time);
        }
        private IReadOnlyList<Vector3[]> ReadSceneRoads()
        {
            fallbackRoads.Clear(); Transform root = GameObject.Find("Game/Paths")?.transform;
            if (root == null) return fallbackRoads;
            foreach (Transform path in root)
            {
                List<Transform> joints = new();
                foreach (Transform joint in path) if (joint.name.StartsWith("Joint_")) joints.Add(joint);
                joints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                Vector3[] points = new Vector3[joints.Count];
                for (int i = 0; i < points.Length; i++) points[i] = joints[i].position;
                if (points.Length > 1) fallbackRoads.Add(points);
            }
            return fallbackRoads;
        }
    }
}
