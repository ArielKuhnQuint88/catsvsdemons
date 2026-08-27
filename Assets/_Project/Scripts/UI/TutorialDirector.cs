using CatsVsDemons.Defense;
using CatsVsDemons.Enemies;
using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.UI
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        private const string CompletedKey = "TutorialCompletedV1";
        private enum Step
        {
            Move,
            SelectLantern,
            Build,
            PowerLantern,
            Fight,
            Special,
            Done
        }

        public static bool BlockWaves =>
            PlayerPrefs.GetInt(CompletedKey, 0) == 0 && !wavesReleased;
        private static bool wavesReleased;

        private ResponsiveCanvasHud hud;
        private KinHealth kin;
        private KinSpecialAttack special;
        private Vector3 movementOrigin;
        private TowerAttack tutorialLantern;
        private Step step;
        private bool selectedDuringTutorial;

        private void Start()
        {
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1)
            {
                wavesReleased = true;
                enabled = false;
                return;
            }

            hud = GetComponent<ResponsiveCanvasHud>();
            kin = Object.FindFirstObjectByType<KinHealth>();
            special = kin != null ? kin.GetComponent<KinSpecialAttack>() : null;
            movementOrigin = kin != null ? kin.transform.position : Vector3.zero;
            TowerBuildSelection.SelectionChanged += OnSelection;
            BuildSpot.DefenseBuilt += OnDefenseBuilt;
            if (special != null) special.Used += OnSpecialUsed;
            SetStep(Step.Move);
        }

        private void OnDestroy()
        {
            TowerBuildSelection.SelectionChanged -= OnSelection;
            BuildSpot.DefenseBuilt -= OnDefenseBuilt;
            if (special != null) special.Used -= OnSpecialUsed;
        }

        private void Update()
        {
            if (kin == null)
            {
                kin = Object.FindFirstObjectByType<KinHealth>();
                return;
            }

            switch (step)
            {
                case Step.Move:
                    Vector3 offset = kin.transform.position - movementOrigin;
                    offset.y = 0f;
                    if (offset.magnitude >= 3f) SetStep(Step.SelectLantern);
                    break;
                case Step.PowerLantern:
                    if (tutorialLantern != null && tutorialLantern.IsPowered)
                    {
                        wavesReleased = true;
                        SetStep(Step.Fight);
                    }
                    break;
                case Step.Fight:
                    KinEnergy energy = kin.GetComponent<KinEnergy>();
                    if (EnemyRegistry.Count > 0 && energy != null && energy.IsFull)
                        SetStep(Step.Special);
                    break;
            }
        }

        private void OnSelection(DefenseType type)
        {
            if (step != Step.SelectLantern || type != DefenseType.Lantern) return;
            selectedDuringTutorial = true;
            SetStep(Step.Build);
        }

        private void OnDefenseBuilt(DefenseType type, GameObject defense)
        {
            if (step != Step.Build || !selectedDuringTutorial ||
                type != DefenseType.Lantern) return;
            tutorialLantern = defense != null
                ? defense.GetComponent<TowerAttack>() : null;
            SetStep(Step.PowerLantern);
        }

        private void OnSpecialUsed()
        {
            if (step != Step.Special) return;
            SetStep(Step.Done);
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            Invoke(nameof(Finish), 3f);
        }

        private void SetStep(Step next)
        {
            step = next;
            switch (step)
            {
                case Step.Move:
                    Show("1/6 — MOVIMENTE KIN",
                        "Use o joystick no celular ou WASD/setas no computador.",
                        "Ande pelo menos 3 metros.");
                    break;
                case Step.SelectLantern:
                    Show("2/6 — ESCOLHA UMA DEFESA",
                        "A Lanterna reduz a velocidade dos demônios.",
                        "Toque no botão LANTERNA.");
                    break;
                case Step.Build:
                    Show("3/6 — CONSTRUA",
                        "As bases douradas recebem as defesas selecionadas.",
                        "Toque em uma base dourada.");
                    break;
                case Step.PowerLantern:
                    Show("4/6 — PODER DE PROXIMIDADE",
                        "Kin aumenta o alcance e libera ondas de fogo na Lanterna.",
                        "Aproxime Kin da Lanterna até ela acender.");
                    break;
                case Step.Fight:
                    Show("5/6 — DEFENDA A CASA",
                        "Kin ataca automaticamente de perto e acumula energia.",
                        "Enfrente os demônios até completar a barra dourada.");
                    break;
                case Step.Special:
                    Show("6/6 — GOLPE SAMURAI",
                        "O golpe circular atinge todos os demônios próximos.",
                        "Toque em GOLPE PRONTO ou pressione Espaço.");
                    break;
                case Step.Done:
                    Show("TREINAMENTO CONCLUÍDO!",
                        "Agora escolha pessoalmente qual caminho precisa de Kin.",
                        "A batalha começou.");
                    break;
            }
        }

        private void Show(string title, string body, string action) =>
            hud?.ShowTutorial(title, body, action);

        private void Finish()
        {
            hud?.HideTutorial();
            enabled = false;
        }
    }
}
