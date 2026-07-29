using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
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
                currentWave = waves.CurrentWave;
                totalWaves = waves.TotalWaves;
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
                waves.WaveStarted -= HandleWaveStarted;
                waves.PreparationChanged -= HandlePreparation;
                waves.PreparationEnded -= HandlePreparationEnded;
                waves.Victory -= HandleVictory;
            }

            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
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
                DrawResult("A CASA CAIU!");
            }
            else if (kinDown)
            {
                DrawResult("KIN CAIU!");
            }
            else if (victory)
            {
                DrawResult("VITÓRIA!");
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

            GUI.Box(new Rect(18f, 18f, 680f, 320f), GUIContent.none);
            GUI.Label(
                new Rect(34f, 28f, 500f, 38f),
                $"Moedas: {coins}",
                coinStyle
            );
            GUI.Label(
                new Rect(34f, 64f, 500f, 31f),
                $"Casa: {houseHealth}/{houseMax}",
                healthStyle
            );
            GUI.Label(
                new Rect(34f, 96f, 500f, 31f),
                $"Kin: {kinHealth}/{kinMax}",
                healthStyle
            );
            GUI.Label(
                new Rect(34f, 132f, 500f, 30f),
                GetWaveText(),
                helpStyle
            );
            GUI.Label(
                new Rect(34f, 166f, 500f, 28f),
                $"Selecionado: {TowerBuildSelection.GetDisplayName()} " +
                $"({TowerBuildSelection.GetCost()})",
                helpStyle
            );

            int previousButtonSize = GUI.skin.button.fontSize;
            FontStyle previousButtonStyle = GUI.skin.button.fontStyle;
            GUI.skin.button.fontSize = 20;
            GUI.skin.button.fontStyle = FontStyle.Bold;

            DrawDefenseButton(
                new Rect(34f, 205f, 200f, 70f),
                "LANTERNA\n10 moedas",
                DefenseType.Lantern,
                new Color(1f, 0.42f, 0.06f)
            );
            DrawDefenseButton(
                new Rect(250f, 205f, 200f, 70f),
                "BONSAI\n15 moedas",
                DefenseType.Bonsai,
                new Color(0.15f, 0.72f, 0.22f)
            );
            DrawDefenseButton(
                new Rect(466f, 205f, 200f, 70f),
                "PORTAL\n10 moedas",
                DefenseType.Portal,
                new Color(0.08f, 0.55f, 1f)
            );

            GUI.skin.button.fontSize = previousButtonSize;
            GUI.skin.button.fontStyle = previousButtonStyle;

            GUI.Label(
                new Rect(34f, 290f, 620f, 24f),
                "Escolha e clique em um ponto livre.",
                helpStyle
            );
        }

        private void DrawDefenseButton(
            Rect area,
            string label,
            DefenseType type,
            Color color)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor =
                TowerBuildSelection.Selected == type
                    ? Color.Lerp(color, Color.white, 0.25f)
                    : color;

            if (GUI.Button(area, label))
            {
                TowerBuildSelection.Select(type);
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawCountdown()
        {
            GUI.Label(
                new Rect(
                    0f,
                    (Screen.height - 130f) * 0.5f,
                    Screen.width,
                    130f
                ),
                preparationSeconds.ToString(),
                countdownStyle
            );
        }

        private void DrawResult(string title)
        {
            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.36f, Screen.width, 90f),
                title,
                resultStyle
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.5f, Screen.width, 60f),
                "A floresta ainda precisa de Kin.",
                messageStyle
            );

            if (GUI.Button(
                new Rect(
                    (Screen.width - 220f) * 0.5f,
                    Screen.height * 0.6f,
                    220f,
                    52f
                ),
                "Reiniciar"))
            {
                RestartGame();
            }
        }

        private void DrawPauseButton()
        {
            if (GUI.Button(
                new Rect(Screen.width - 130f, 22f, 105f, 42f),
                paused ? "Continuar" : "Pausar"))
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void DrawPauseMenu()
        {
            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.34f, Screen.width, 90f),
                "PAUSADO",
                resultStyle
            );

            if (GUI.Button(
                new Rect(
                    (Screen.width - 220f) * 0.5f,
                    Screen.height * 0.5f,
                    220f,
                    52f
                ),
                "Continuar"))
            {
                paused = false;
                Time.timeScale = 1f;
            }

            if (GUI.Button(
                new Rect(
                    (Screen.width - 220f) * 0.5f,
                    Screen.height * 0.5f + 66f,
                    220f,
                    52f
                ),
                "Reiniciar"))
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
                return $"Onda {currentWave} começa em: {preparationSeconds}s";
            }

            return $"Onda: {currentWave}/{totalWaves}";
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
}
