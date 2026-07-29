using CatsVsDemons.Economy;
using CatsVsDemons.House;
using UnityEngine;

namespace CatsVsDemons.UI
{
    public sealed class CoinHud : MonoBehaviour
    {
        private Wallet wallet;
        private HouseHealth house;
        private GUIStyle coinStyle;
        private GUIStyle healthStyle;
        private GUIStyle helpStyle;
        private GUIStyle gameOverStyle;
        private bool gameOver;

        private void Awake()
        {
            wallet = Object.FindFirstObjectByType<Wallet>();
            house = Object.FindFirstObjectByType<HouseHealth>();

            coinStyle = CreateStyle(
                28,
                new Color(1f, 0.82f, 0.16f),
                FontStyle.Bold
            );
            healthStyle = CreateStyle(
                24,
                new Color(0.35f, 1f, 0.45f),
                FontStyle.Bold
            );
            helpStyle = CreateStyle(17, Color.white, FontStyle.Normal);
            gameOverStyle = CreateStyle(52, Color.white, FontStyle.Bold);
            gameOverStyle.alignment = TextAnchor.MiddleCenter;
        }

        private void Start()
        {
            if (house == null)
            {
                house = Object.FindFirstObjectByType<HouseHealth>();
            }

            if (house != null)
            {
                house.Destroyed += HandleGameOver;
            }
        }

        private void OnDestroy()
        {
            if (house != null)
            {
                house.Destroyed -= HandleGameOver;
            }

            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            DrawStatusPanel();

            if (gameOver)
            {
                DrawGameOver();
            }
        }

        private void DrawStatusPanel()
        {
            int coins = wallet != null ? wallet.Coins : 0;
            int currentHealth = house != null ? house.CurrentHealth : 0;
            int maxHealth = house != null ? house.MaxHealth : 0;

            GUI.Box(new Rect(18f, 18f, 390f, 132f), GUIContent.none);
            GUI.Label(
                new Rect(34f, 28f, 350f, 38f),
                $"Moedas: {coins}",
                coinStyle
            );
            GUI.Label(
                new Rect(34f, 64f, 350f, 34f),
                $"Casa: {currentHealth}/{maxHealth}",
                healthStyle
            );
            GUI.Label(
                new Rect(34f, 108f, 350f, 28f),
                "Clique em um ponto para construir (10)",
                helpStyle
            );
        }

        private void DrawGameOver()
        {
            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.36f, Screen.width, 90f),
                "A CASA CAIU!",
                gameOverStyle
            );

            GUIStyle messageStyle =
                CreateStyle(22, Color.white, FontStyle.Normal);
            messageStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(
                new Rect(0f, Screen.height * 0.5f, Screen.width, 60f),
                "Pare e aperte Play para tentar novamente.",
                messageStyle
            );
        }

        private void HandleGameOver()
        {
            gameOver = true;
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
