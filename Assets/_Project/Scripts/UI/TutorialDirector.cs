using CatsVsDemons.CameraSystem;
using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
using CatsVsDemons.Enemies;
using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.UI
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        private const string CompletedKey = "TutorialCompletedV2";
        private enum Step { Move, SelectLantern, Build, PowerLantern, Fight, Special, Done }
        private static bool wavesReleased;
        public static bool BlockWaves => PlayerPrefs.GetInt(CompletedKey, 0) == 0 && !wavesReleased;
        private ResponsiveCanvasHud hud;
        private CameraModeController cameraController;
        private KinHealth kin;
        private KinEnergy energy;
        private KinSpecialAttack special;
        private Vector3 movementOrigin;
        private TowerAttack tutorialLantern;
        private BuildSpot buildSpot;
        private Transform enemyTarget;
        private Step step;
        private bool initialized;
        private bool replay;
        private float nextTargetRefresh;
        private float finishedTime;
        private int progressValue = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => wavesReleased = false;

        public static void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.DeleteKey("TutorialCompletedV1");
            PlayerPrefs.Save();
            wavesReleased = false;
        }

        private void Start()
        {
            if (replay) return;
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1)
            {
                wavesReleased = true;
                enabled = false;
                return;
            }
            wavesReleased = false;
            hud = GetComponent<ResponsiveCanvasHud>();
            cameraController = Object.FindFirstObjectByType<CameraModeController>();
            TryInitialize();
        }

        public void Replay()
        {
            Unsubscribe();
            TowerBuildSelection.CancelPlacement();
            hud = GetComponent<ResponsiveCanvasHud>();
            cameraController = Object.FindFirstObjectByType<CameraModeController>();
            initialized = false;
            tutorialLantern = null;
            buildSpot = null;
            enemyTarget = null;
            replay = true;
            enabled = true;
            TryInitialize();
        }

        private void TryInitialize()
        {
            kin = Object.FindFirstObjectByType<KinHealth>();
            if (kin == null || hud == null) return;
            energy = kin.GetComponent<KinEnergy>();
            special = kin.GetComponent<KinSpecialAttack>();
            if (energy == null || special == null) return;
            movementOrigin = kin.transform.position;
            TowerBuildSelection.SelectionChanged += OnSelection;
            BuildSpot.DefenseBuilt += OnDefenseBuilt;
            special.Used += OnSpecialUsed;
            initialized = true;
            SetStep(Step.Move);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            cameraController?.ClearTutorialFocus();
        }

        private void Unsubscribe()
        {
            TowerBuildSelection.SelectionChanged -= OnSelection;
            BuildSpot.DefenseBuilt -= OnDefenseBuilt;
            if (special != null) special.Used -= OnSpecialUsed;
        }

        private void Update()
        {
            if (!initialized) { TryInitialize(); return; }
            if (kin == null || !hud.CanGuideTutorial || Time.timeScale <= 0) return;
            switch (step)
            {
                case Step.Move:
                    Vector3 offset = kin.transform.position - movementOrigin;
                    offset.y = 0;
                    SetProgress(Mathf.Min(30, Mathf.FloorToInt(offset.magnitude * 10)), 30, "Movimento");
                    if (offset.magnitude >= 3f) SetStep(Step.SelectLantern);
                    break;
                case Step.Build:
                    if (buildSpot == null || buildSpot.IsOccupied)
                    {
                        buildSpot = FindBuildSpot();
                        Focus(TutorialFocus.BuildSpot, buildSpot != null ? buildSpot.transform : null, "CONSTRUA AQUI");
                    }
                    break;
                case Step.PowerLantern:
                    if (tutorialLantern == null) { SetStep(Step.Build); break; }
                    if (tutorialLantern.IsPowered)
                    {
                        wavesReleased = true;
                        SetStep(Step.Fight);
                    }
                    break;
                case Step.Fight:
                    SetProgress(Mathf.RoundToInt(energy.Normalized * 100), 100, "Energia");
                    if (Time.unscaledTime >= nextTargetRefresh)
                    {
                        nextTargetRefresh = Time.unscaledTime + 0.4f;
                        EnemyHealth nearest = EnemyRegistry.FindNearest(kin.transform.position, 150f);
                        Transform next = nearest != null ? nearest.transform : null;
                        if (next != enemyTarget)
                        {
                            enemyTarget = next;
                            // The alert follows enemies; the camera stays near Kin, not distant entrances.
                            hud.SetTutorialFocus(TutorialFocus.Enemy, next, "APROXIME-SE DO DEMÔNIO");
                        }
                    }
                    if (energy.IsFull) SetStep(Step.Special);
                    break;
                case Step.Done:
                    finishedTime -= Time.unscaledDeltaTime;
                    if (finishedTime <= 0) Finish();
                    break;
            }
        }

        private void SetProgress(int value, int maximum, string label)
        {
            if (value == progressValue) return;
            progressValue = value;
            hud.SetTutorialProgress($"{label}: {Mathf.RoundToInt(value * 100f / maximum)}%");
        }

        private void OnSelection(DefenseType type)
        {
            if (step == Step.SelectLantern && type == DefenseType.Lantern) SetStep(Step.Build);
            else if (step == Step.Build && type != DefenseType.Lantern) SetStep(Step.SelectLantern);
        }

        private void OnDefenseBuilt(DefenseType type, GameObject defense)
        {
            if (step != Step.Build || type != DefenseType.Lantern || defense == null) return;
            tutorialLantern = defense.GetComponent<TowerAttack>();
            SetStep(Step.PowerLantern);
        }

        private void OnSpecialUsed()
        {
            if (step != Step.Special) return;
            Complete();
            SetStep(Step.Done);
            finishedTime = 3f;
        }

        public void Skip()
        {
            Complete();
            Finish();
        }

        private static void Complete()
        {
            wavesReleased = true;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        private void Finish()
        {
            Unsubscribe();
            step = Step.Done;
            hud?.HideTutorial();
            cameraController?.ClearTutorialFocus();
            enabled = false;
        }

        private void Focus(TutorialFocus kind, Transform target, string caption, bool zoom = true)
        {
            hud.SetTutorialFocus(kind, target, caption);
            if (zoom && target != null) cameraController?.FocusTutorial(target);
            else cameraController?.ClearTutorialFocus();
        }

        private BuildSpot FindBuildSpot()
        {
            BuildSpot nearest = null;
            float distance = float.MaxValue;
            foreach (BuildSpot spot in Object.FindObjectsByType<BuildSpot>(FindObjectsSortMode.None))
            {
                if (spot.IsOccupied) continue;
                float sqr = (spot.transform.position - kin.transform.position).sqrMagnitude;
                if (sqr < distance) { nearest = spot; distance = sqr; }
            }
            return nearest;
        }

        private void SetStep(Step next)
        {
            step = next; progressValue = -1;
            switch (step)
            {
                case Step.Move:
                    Show("1/6 • VAMOS ANDAR", "Sou Kin, o guardião desta casa! Venha comigo conhecer o jardim.",
                        Application.isMobilePlatform ? "Use o joystick e ande 3 metros." : "Use WASD ou as setas e ande 3 metros.");
                    Focus(TutorialFocus.Movement, kin.transform, "MOVIMENTE KIN");
                    break;
                case Step.SelectLantern:
                    Show("2/6 • MINHA PRIMEIRA DEFESA", "Uma Lanterna deixa os demônios lentos. Vamos construir uma!",
                        "Clique ou toque no botão LANTERNA destacado.");
                    Focus(TutorialFocus.LanternButton, null, "ESCOLHA LANTERNA");
                    break;
                case Step.Build:
                    if (TowerBuildSelection.Selected != DefenseType.Lantern || !TowerBuildSelection.AwaitingPlacement)
                        TowerBuildSelection.Select(DefenseType.Lantern);
                    Wallet wallet = Object.FindFirstObjectByType<Wallet>();
                    int cost = TowerBuildSelection.GetCost();
                    if (!replay && wallet != null && wallet.Coins < cost) wallet.AddCoins(cost - wallet.Coins);
                    Show("3/6 • CONSTRUA COMIGO", "As bases iluminadas são os lugares onde posso instalar nossas defesas.",
                        replay ? "Clique na base marcada. Se faltarem moedas, derrote demônios para juntar o custo."
                            : "Clique ou toque na base marcada. Você já tem as moedas!");
                    buildSpot = FindBuildSpot();
                    if (replay && buildSpot == null)
                    {
                        tutorialLantern = Object.FindFirstObjectByType<TowerAttack>();
                        if (tutorialLantern != null) { SetStep(Step.PowerLantern); return; }
                    }
                    Focus(TutorialFocus.BuildSpot, buildSpot != null ? buildSpot.transform : null, "CONSTRUA AQUI");
                    break;
                case Step.PowerLantern:
                    Show("4/6 • ACENDA MEU PODER", "Perto de mim, a Lanterna fica mais forte e lança ondas de fogo!",
                        "Leve-me até a Lanterna marcada para fazê-la acender.");
                    Focus(TutorialFocus.LanternPower, tutorialLantern != null ? tutorialLantern.transform : null, "VENHA ATÉ A LANTERNA");
                    break;
                case Step.Fight:
                    Show("5/6 • ELES ESTÃO CHEGANDO", "Eu ataco sozinho quando chego perto de um demônio. Cada golpe carrega minha energia.",
                        "Aguarde a onda e aproxime-me dos demônios marcados.");
                    Focus(TutorialFocus.Enemy, null, "ENFRENTE O DEMÔNIO", false);
                    cameraController?.FocusTutorial(kin.transform, 0.88f);
                    break;
                case Step.Special:
                    Show("6/6 • GOLPE SAMURAI", "Minha energia está cheia! Posso atingir os demônios próximos com um só golpe.",
                        Application.isMobilePlatform ? "Toque no botão GOLPE PRONTO destacado." : "Clique em GOLPE PRONTO ou pressione Espaço.");
                    Focus(TutorialFocus.SpecialButton, null, "USE O GOLPE");
                    break;
                case Step.Done:
                    Show("AGORA SOMOS UMA EQUIPE!", "Você aprendeu a andar, construir, fortalecer torres e lutar comigo.",
                        "Proteja a casa. Eu conto com você!");
                    hud.SetTutorialFocus(TutorialFocus.None, null, "");
                    cameraController?.FocusTutorial(kin.transform, 0.8f);
                    break;
            }
        }

        private void Show(string title, string body, string action) => hud.ShowTutorial(title, body, action);
    }
}
