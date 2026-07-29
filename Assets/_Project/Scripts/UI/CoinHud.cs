using CatsVsDemons.Economy;
using CatsVsDemons.House;
using CatsVsDemons.Player;
using CatsVsDemons.Waves;
using UnityEngine;

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
        private int currentWave;
        private int totalWaves;
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
                28,
                new Color(1f, 0.82f, 0.16f),
                FontStyle.Bold
            );
            healthStyle = CreateStyle(
                22,
                new Color(0.35f, 1f, 0.45f),
                FontStyle.Bold
            );
            helpStyle = CreateStyle(17, Color.white, FontStyle.Normal);
            resultStyle = CreateStyle(52, Color.white, FontStyle.Bold);
            resultStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle = CreateStyle(22, Color.white, FontStyle.Normal);
            messageStyle.alignment = TextAnchor.MiddleCenter;
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
                waves.Victory -= HandleVictory;
            }

            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            DrawStatusPanel();

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
        }

        private void DrawStatusPanel()
        {
            int coins = wallet != null ? wallet.Coins : 0;
            int houseHealth = house != null ? house.CurrentHealth : 0;
            int houseMax = house != null ? house.MaxHealth : 0;
            int kinHealth = kin != null ? kin.CurrentHealth : 0;
            int kinMax = kin != null ? kin.MaxHealth : 0;

            GUI.Box(new Rect(18f, 18f, 390f, 202f), GUIContent.none);
            GUI.Label(
                new Rect(34f, 28f, 350f, 38f),
                $"Moedas: {coins}",
                coinStyle
            );
            GUI.Label(
                new Rect(34f, 64f, 350f, 31f),
                $"Casa: {houseHealth}/{houseMax}",
                healthStyle
            );
            GUI.Label(
                new Rect(34f, 96f, 350f, 31f),
                $"Kin: {kinHealth}/{kinMax}",
                healthStyle
            );
            GUI.Label(
                new Rect(34f, 132f, 350f, 30f),
                $"Onda: {currentWave}/{totalWaves}",
                helpStyle
            );
            GUI.Label(
                new Rect(34f, 174f, 350f, 28f),
                "Clique em um ponto para construir (10)",
                helpStyle
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
                "Pare e aperte Play para tentar novamente.",
                messageStyle
            );
        }

        private void HandleWaveStarted(int wave, int total)
        {
            currentWave = wave;
            totalWaves = total;
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
