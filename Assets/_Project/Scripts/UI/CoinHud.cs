using CatsVsDemons.Economy;
using UnityEngine;

namespace CatsVsDemons.UI
{
    public sealed class CoinHud : MonoBehaviour
    {
        private Wallet wallet;
        private GUIStyle coinStyle;
        private GUIStyle helpStyle;

        private void Awake()
        {
            wallet = Object.FindFirstObjectByType<Wallet>();

            coinStyle = new GUIStyle
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.82f, 0.16f) }
            };

            helpStyle = new GUIStyle
            {
                fontSize = 17,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            int coins = wallet != null ? wallet.Coins : 0;

            GUI.Box(new Rect(18f, 18f, 360f, 92f), GUIContent.none);
            GUI.Label(new Rect(34f, 28f, 320f, 38f), $"Moedas: {coins}", coinStyle);
            GUI.Label(
                new Rect(34f, 68f, 320f, 28f),
                "Clique em um ponto para construir (10)",
                helpStyle
            );
        }
    }
}
