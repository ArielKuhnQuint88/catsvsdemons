using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsVsDemons.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField] private Texture2D comicTexture;
        private enum Panel
        {
            None,
            Credits,
            Story,
            Settings
        }

        private Panel activePanel;
        private GUIStyle titleStyle;
        private GUIStyle panelTitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle comicCaptionStyle;
        private bool showingIntro;
        private int comicPanel;
        private int pendingCameraMode;

        private static readonly string[] ComicCaptions =
        {
            "Durante o dia, Kin dorme. E dorme muito.",
            "Seu dono suspira: \"Esse gato é um preguiçoso...\"",
            "Mas, quando o sol se põe, o verdadeiro trabalho de Kin começa.",
            "Os demônios despertam e avançam em direção à casa.",
            "Sem que seu dono saiba, Kin se torna o guardião da noite!",
            "Ao amanhecer, a casa está segura. E Kin? Dormindo outra vez."
        };

        public void SetBackground(Texture2D texture)
        {
            backgroundTexture = texture;
        }

        private void Awake()
        {
            if (backgroundTexture == null)
            {
                backgroundTexture =
                    Resources.Load<Texture2D>("UI/OpeningBackground");
            }

            if (comicTexture == null)
            {
                comicTexture = Resources.Load<Texture2D>("UI/IntroComic");
            }

            Time.timeScale = 1f;

            titleStyle = CreateStyle(
                54,
                new Color(1f, 0.78f, 0.12f),
                FontStyle.Bold,
                TextAnchor.MiddleCenter
            );
            panelTitleStyle = CreateStyle(
                34,
                Color.white,
                FontStyle.Bold,
                TextAnchor.MiddleCenter
            );
            bodyStyle = CreateStyle(
                20,
                Color.white,
                FontStyle.Normal,
                TextAnchor.UpperCenter
            );
            bodyStyle.wordWrap = true;
            comicCaptionStyle = CreateStyle(
                30,
                Color.white,
                FontStyle.Bold,
                TextAnchor.MiddleCenter
            );
            comicCaptionStyle.wordWrap = true;
        }

        private void OnGUI()
        {
            if (showingIntro)
            {
                DrawComicIntro();
                return;
            }

            DrawBackground();
            DrawTitle();

            if (activePanel == Panel.None)
            {
                DrawModeButtons();
                DrawBottomButtons();
            }
            else
            {
                DrawPanel();
            }
        }

        private void DrawBackground()
        {
            Rect screen = new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height
            );

            if (backgroundTexture != null)
            {
                GUI.DrawTexture(
                    screen,
                    backgroundTexture,
                    ScaleMode.ScaleAndCrop
                );
                return;
            }

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.08f, 0.18f);
            GUI.Box(screen, GUIContent.none);
            GUI.backgroundColor = previous;
        }

        private void DrawTitle()
        {
            GUI.Label(
                new Rect(0f, 45f, Screen.width, 85f),
                "CATS VS DEMONS",
                titleStyle
            );
        }

        private void DrawModeButtons()
        {
            float width = Mathf.Min(520f, Screen.width * 0.55f);
            float height = 92f;
            float left = (Screen.width - width) * 0.5f;
            float top = Screen.height * 0.31f;

            int oldSize = GUI.skin.button.fontSize;
            FontStyle oldStyle = GUI.skin.button.fontStyle;
            GUI.skin.button.fontSize = 28;
            GUI.skin.button.fontStyle = FontStyle.Bold;

            Color previous = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.12f);
            if (GUI.Button(
                new Rect(left, top, width, height),
                "ISOMÉTRICO"))
            {
                BeginIntro(0);
            }

            GUI.backgroundColor = new Color(0.16f, 0.55f, 0.95f);
            if (GUI.Button(
                new Rect(left, top + 112f, width, height),
                "PRIMEIRA PESSOA"))
            {
                BeginIntro(1);
            }

            GUI.backgroundColor = previous;
            GUI.skin.button.fontSize = oldSize;
            GUI.skin.button.fontStyle = oldStyle;
        }

        private void BeginIntro(int cameraMode)
        {
            pendingCameraMode = cameraMode;
            comicPanel = 0;
            showingIntro = comicTexture != null;

            if (!showingIntro)
            {
                StartGame(cameraMode);
            }
        }

        private void DrawComicIntro()
        {
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.blackTexture,
                ScaleMode.StretchToFill
            );

            int column = comicPanel % 3;
            int row = comicPanel / 3;
            Rect textureCoordinates = new Rect(
                column / 3f,
                row == 0 ? 0.5f : 0f,
                1f / 3f,
                0.5f
            );

            float imageAspect = 32f / 27f;
            float availableHeight = Screen.height * 0.78f;
            float imageWidth = Mathf.Min(
                Screen.width,
                availableHeight * imageAspect
            );
            float imageHeight = imageWidth / imageAspect;
            Rect imageArea = new Rect(
                (Screen.width - imageWidth) * 0.5f,
                0f,
                imageWidth,
                imageHeight
            );
            GUI.DrawTextureWithTexCoords(
                imageArea,
                comicTexture,
                textureCoordinates,
                true
            );

            Rect captionArea = new Rect(
                Screen.width * 0.08f,
                Screen.height * 0.76f,
                Screen.width * 0.84f,
                Screen.height * 0.12f
            );
            Color previous = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.94f);
            GUI.DrawTexture(captionArea, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(captionArea, ComicCaptions[comicPanel], comicCaptionStyle);

            float buttonY = Screen.height - 72f;
            if (GUI.Button(
                new Rect(24f, buttonY, 150f, 48f),
                "PULAR"))
            {
                StartGame(pendingCameraMode);
            }

            string nextLabel = comicPanel < ComicCaptions.Length - 1
                ? "PRÓXIMO"
                : "DEFENDER A CASA!";
            if (GUI.Button(
                new Rect(Screen.width - 254f, buttonY, 230f, 48f),
                nextLabel))
            {
                if (comicPanel < ComicCaptions.Length - 1)
                {
                    comicPanel++;
                }
                else
                {
                    StartGame(pendingCameraMode);
                }
            }
        }

        private void DrawBottomButtons()
        {
            string[] labels =
            {
                "Créditos",
                "História",
                "Configurações",
                "Sair"
            };

            float buttonWidth = 165f;
            float gap = 16f;
            float totalWidth =
                (buttonWidth * labels.Length) +
                (gap * (labels.Length - 1));
            float left = (Screen.width - totalWidth) * 0.5f;
            float top = Screen.height - 100f;

            int oldSize = GUI.skin.button.fontSize;
            GUI.skin.button.fontSize = 17;

            for (int index = 0; index < labels.Length; index++)
            {
                if (!GUI.Button(
                    new Rect(
                        left + index * (buttonWidth + gap),
                        top,
                        buttonWidth,
                        48f
                    ),
                    labels[index]))
                {
                    continue;
                }

                switch (index)
                {
                    case 0:
                        activePanel = Panel.Credits;
                        break;
                    case 1:
                        activePanel = Panel.Story;
                        break;
                    case 2:
                        activePanel = Panel.Settings;
                        break;
                    default:
                        QuitGame();
                        break;
                }
            }

            GUI.skin.button.fontSize = oldSize;
        }

        private void DrawPanel()
        {
            float width = Mathf.Min(680f, Screen.width * 0.75f);
            float height = Mathf.Min(390f, Screen.height * 0.58f);
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.22f,
                width,
                height
            );

            GUI.Box(panel, GUIContent.none);

            string title = activePanel == Panel.Credits
                ? "CRÉDITOS"
                : activePanel == Panel.Story
                    ? "HISTÓRIA"
                    : "CONFIGURAÇÕES";

            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 24f, width - 40f, 48f),
                title,
                panelTitleStyle
            );

            if (activePanel == Panel.Credits)
            {
                GUI.Label(
                    new Rect(
                        panel.x + 50f,
                        panel.y + 100f,
                        width - 100f,
                        180f
                    ),
                    "Criação, game design e programação\n" +
                    "Ariel Kühn Quint\n\n" +
                    "Produção independente — Quint / QiP Games",
                    bodyStyle
                );
            }
            else if (activePanel == Panel.Story)
            {
                GUI.Label(
                    new Rect(
                        panel.x + 45f,
                        panel.y + 92f,
                        width - 90f,
                        210f
                    ),
                    "Demônios avançam por caminhos encantados em direção " +
                    "à casa oriental. Kin, o gato samurai, deve proteger " +
                    "o lar, construir defesas e usar portais antes que " +
                    "a floresta seja dominada.",
                    bodyStyle
                );
            }
            else
            {
                DrawSettings(panel);
            }

            if (GUI.Button(
                new Rect(
                    panel.x + (width - 180f) * 0.5f,
                    panel.yMax - 70f,
                    180f,
                    44f
                ),
                "Voltar"))
            {
                activePanel = Panel.None;
            }
        }

        private void DrawSettings(Rect panel)
        {
            GUI.Label(
                new Rect(panel.x + 80f, panel.y + 105f, 160f, 30f),
                "Volume",
                bodyStyle
            );

            AudioListener.volume = GUI.HorizontalSlider(
                new Rect(panel.x + 250f, panel.y + 117f, 300f, 24f),
                AudioListener.volume,
                0f,
                1f
            );

            bool fullscreen = Screen.fullScreen;
            bool newFullscreen = GUI.Toggle(
                new Rect(panel.x + 200f, panel.y + 175f, 280f, 32f),
                fullscreen,
                " Tela cheia"
            );

            if (newFullscreen != fullscreen)
            {
                Screen.fullScreen = newFullscreen;
            }
        }

        private static void StartGame(int cameraMode)
        {
            PlayerPrefs.SetInt("CameraMode", cameraMode);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static GUIStyle CreateStyle(
            int size,
            Color color,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            return new GUIStyle
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                normal = { textColor = color }
            };
        }
    }
}
