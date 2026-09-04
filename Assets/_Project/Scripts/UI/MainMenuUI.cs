using UnityEngine;
using UnityEngine.SceneManagement;
using CatsVsDemons.Waves;

namespace CatsVsDemons.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Texture2D backgroundTexture;
        private readonly Texture2D[] comicScenes = new Texture2D[5];
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
            "Sem que seu dono saiba, Kin se torna o guardião da noite!"
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

            for (int index = 0; index < comicScenes.Length; index++)
            {
                comicScenes[index] = LoadStoryTexture(index + 1);
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
            bool mobile = ResponsiveGuiTheme.IsMobile;
            float scale = ResponsiveGuiTheme.LayoutScale;
            float width = mobile
                ? Mathf.Min(Screen.width * 0.46f, 520f * scale)
                : Mathf.Min(Screen.width * 0.30f, 380f * scale);
            float height = (mobile ? 68f : 58f) * scale;
            float gap = (mobile ? 18f : 14f) * scale;
            float left = (Screen.width - width) * 0.5f;
            float top = Screen.height * 0.31f;
            int fontSize = Mathf.RoundToInt((mobile ? 23f : 20f) * scale);

            if (ResponsiveGuiTheme.Button(
                new Rect(left, top, width, height),
                "ISOMÉTRICO",
                ResponsiveGuiTheme.ButtonTone.Crimson,
                fontSize))
            {
                BeginIntro(0);
            }

            if (ResponsiveGuiTheme.Button(
                new Rect(left, top + height + gap, width, height),
                "PRIMEIRA PESSOA",
                ResponsiveGuiTheme.ButtonTone.Azure,
                fontSize))
            {
                BeginIntro(1);
            }
        }

        private void BeginIntro(int cameraMode)
        {
            pendingCameraMode = cameraMode;
            comicPanel = 0;
            showingIntro = comicScenes[0] != null;

            if (!showingIntro)
            {
                StartGame(cameraMode);
            }
        }

        private void DrawComicIntro()
        {
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                comicScenes[comicPanel],
                ScaleMode.ScaleAndCrop
            );

            bool mobile = ResponsiveGuiTheme.IsMobile;
            float scale = ResponsiveGuiTheme.LayoutScale;
            Rect captionArea = new Rect(
                Screen.width * 0.06f,
                Screen.height * 0.73f,
                Screen.width * 0.88f,
                Screen.height * 0.14f
            );
            Color previous = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.94f);
            GUI.DrawTexture(captionArea, Texture2D.whiteTexture);
            GUI.color = previous;

            comicCaptionStyle.fontSize = Mathf.RoundToInt(
                Mathf.Clamp((mobile ? 25f : 27f) * scale, 24f, 42f)
            );
            GUI.Label(captionArea, ComicCaptions[comicPanel], comicCaptionStyle);

            float margin = (mobile ? 28f : 24f) * scale;
            float buttonHeight = (mobile ? 64f : 48f) * scale;
            float buttonY = Screen.height - buttonHeight - margin;
            float skipWidth = (mobile ? 190f : 132f) * scale;
            float nextWidth = (mobile ? 280f : 210f) * scale;
            int fontSize = Mathf.RoundToInt((mobile ? 22f : 17f) * scale);

            if (ResponsiveGuiTheme.Button(
                new Rect(margin, buttonY, skipWidth, buttonHeight),
                "PULAR",
                ResponsiveGuiTheme.ButtonTone.Ink,
                fontSize))
            {
                StartGame(pendingCameraMode);
            }

            string nextLabel = comicPanel < ComicCaptions.Length - 1
                ? "PRÓXIMO"
                : "DEFENDER A CASA!";
            if (ResponsiveGuiTheme.Button(
                new Rect(
                    Screen.width - nextWidth - margin,
                    buttonY,
                    nextWidth,
                    buttonHeight
                ),
                nextLabel,
                ResponsiveGuiTheme.ButtonTone.Gold,
                fontSize))
            {
                if (comicPanel < ComicCaptions.Length - 1)
                    comicPanel++;
                else
                    StartGame(pendingCameraMode);
            }
        }

        private static Texture2D LoadStoryTexture(int sceneNumber)
        {
            TextAsset image = Resources.Load<TextAsset>(
                $"UI/IntroScene{sceneNumber}Data"
            );
            if (image == null)
            {
                Debug.LogError($"Intro scene {sceneNumber} was not found.");
                return null;
            }

            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGB24,
                false
            )
            {
                name = $"IntroScene{sceneNumber}_Runtime"
            };
            if (ImageConversion.LoadImage(texture, image.bytes, true))
            {
                return texture;
            }

            Object.Destroy(texture);
            return null;
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

            bool mobile = ResponsiveGuiTheme.IsMobile;
            float scale = ResponsiveGuiTheme.LayoutScale;
            float buttonWidth = (mobile ? 176f : 118f) * scale;
            float buttonHeight = (mobile ? 54f : 40f) * scale;
            float gap = (mobile ? 14f : 10f) * scale;
            float totalWidth =
                buttonWidth * labels.Length + gap * (labels.Length - 1);
            float left = (Screen.width - totalWidth) * 0.5f;
            float top = Screen.height - buttonHeight - 24f * scale;
            int fontSize = Mathf.RoundToInt((mobile ? 18f : 14f) * scale);

            for (int index = 0; index < labels.Length; index++)
            {
                if (!ResponsiveGuiTheme.Button(
                    new Rect(
                        left + index * (buttonWidth + gap),
                        top,
                        buttonWidth,
                        buttonHeight
                    ),
                    labels[index],
                    ResponsiveGuiTheme.ButtonTone.Ink,
                    fontSize))
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

            float backScale = ResponsiveGuiTheme.LayoutScale;
            float backWidth = (ResponsiveGuiTheme.IsMobile ? 220f : 160f) * backScale;
            float backHeight = (ResponsiveGuiTheme.IsMobile ? 58f : 42f) * backScale;
            if (ResponsiveGuiTheme.Button(
                new Rect(
                    panel.x + (width - backWidth) * 0.5f,
                    panel.yMax - backHeight - 18f * backScale,
                    backWidth,
                    backHeight
                ),
                "VOLTAR",
                ResponsiveGuiTheme.ButtonTone.Gold,
                Mathf.RoundToInt(17f * backScale)))
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
            CampaignProgress.BeginNewCampaign();
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
