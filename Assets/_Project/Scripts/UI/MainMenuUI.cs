using System.Collections.Generic;
using CatsVsDemons.Waves;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    /// <summary>The title screen uses the same native, touch-ready controls as the garden, house and guide.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Texture2D backgroundTexture;
        private readonly Texture2D[] comicScenes = new Texture2D[5];
        private readonly Sprite[] comicSprites = new Sprite[5];
        private readonly List<Object> owned = new();
        private RuntimeUiFactory ui;
        private RectTransform safeArea, home, modal, comic;
        private RawImage background;
        private Image comicImage;
        private Text comicCaption, comicProgress, nextLabel, tutorialMessage;
        private Button comicNext;
        private CanvasGroup inputGroup;
        private Rect lastSafeArea;
        private int comicPanel, pendingCameraMode;
        private bool showingIntro;
        private enum Panel { None, Credits, Story, Settings }
        private Panel activePanel;
        private GameObject credits, story, settings;
        private static readonly string[] ComicCaptions =
        {
            "Durante o dia, Kin dorme. E dorme muito.",
            "Seu dono suspira: \"Esse gato é um preguiçoso...\"",
            "Mas, quando o sol se põe, o verdadeiro trabalho de Kin começa.",
            "Os demônios despertam e avançam em direção à casa.",
            "Sem que seu dono saiba, Kin se torna o guardião da noite!"
        };
        private static bool GuideBlocksInput => KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame;

        public void SetBackground(Texture2D texture)
        {
            backgroundTexture = texture;
            if (background != null) background.texture = texture;
        }

        private void Awake()
        {
            KinGuideOverlay.Ensure(); Time.timeScale = 1f;
            if (backgroundTexture == null) backgroundTexture = Resources.Load<Texture2D>("UI/OpeningBackground") ??
                Resources.Load<Texture2D>("UI/OpeningBackground 1");
            for (int i = 0; i < comicScenes.Length; i++)
            {
                comicScenes[i] = LoadStoryTexture(i + 1);
                if (comicScenes[i] == null) continue;
                comicSprites[i] = Sprite.Create(comicScenes[i], new Rect(0, 0, comicScenes[i].width, comicScenes[i].height), new Vector2(.5f, .5f));
                owned.Add(comicSprites[i]); owned.Add(comicScenes[i]);
            }
            ui = new RuntimeUiFactory(); Canvas canvas = ui.CreateCanvas(transform);
            canvas.name = "Title Menu Canvas"; canvas.sortingOrder = 400;
            inputGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            RectTransform art = ui.Rect("Opening Artwork", canvas.transform); RuntimeUiFactory.Stretch(art, Vector2.zero);
            background = art.gameObject.AddComponent<RawImage>(); background.texture = backgroundTexture;
            background.color = backgroundTexture != null ? Color.white : RuntimeUiFactory.Ink; background.raycastTarget = false;
            AspectRatioFitter aspect = art.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = backgroundTexture != null ? (float)backgroundTexture.width / backgroundTexture.height : 1.5f;
            RectTransform veil = ui.Panel("Opening Jade Veil", canvas.transform, new Color(.025f, .09f, .075f, .55f), Vector2.zero);
            RuntimeUiFactory.Stretch(veil, Vector2.zero); veil.GetComponent<Image>().raycastTarget = false;
            safeArea = ui.Rect("Menu Safe Area", canvas.transform); RuntimeUiFactory.Stretch(safeArea, Vector2.zero);
            BuildHome(); BuildPanels(); BuildComic(); ApplySafeArea();
        }

        private void BuildHome()
        {
            home = ui.Rect("Home Menu", safeArea); RuntimeUiFactory.Stretch(home, Vector2.zero);
            RectTransform layout = ui.Rect("Title Composition", home); layout.sizeDelta = new Vector2(1000, 800);
            RuntimeUiFactory.Fit(layout, new Vector2(100, 180));
            Text eyebrow = ui.Label("Title Eyebrow", "O GUARDIÃO DA NOITE", layout, 20, RuntimeUiFactory.Gold, new Vector2(780, 40), TextAnchor.MiddleCenter);
            eyebrow.rectTransform.anchoredPosition = new Vector2(0, 316);
            Text title = ui.Label("Game Title", "CATS <size=30>vs</size> DEMONS", layout, 72, RuntimeUiFactory.Paper, new Vector2(960, 110), TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0, 235);
            title.gameObject.AddComponent<Shadow>().effectColor = RuntimeUiFactory.Ink;
            ui.Rule(layout, 300, new Vector2(0, 160));
            Text intro = ui.Label("Menu Subtitle", "Um jardim para proteger. Uma lenda para despertar.", layout, 25, RuntimeUiFactory.Paper, new Vector2(940, 55), TextAnchor.MiddleCenter);
            intro.fontStyle = FontStyle.Normal; intro.rectTransform.anchoredPosition = new Vector2(0, 103);
            Text choose = ui.Label("View Choice", "ESCOLHA SUA VISÃO", layout, 18, RuntimeUiFactory.Gold, new Vector2(800, 40), TextAnchor.MiddleCenter);
            choose.rectTransform.anchoredPosition = new Vector2(0, 22);
            AddModeCard(layout, 0, -230, "ISOMÉTRICO", "Veja o jardim e planeje suas defesas.", UiIcon.Isometric);
            AddModeCard(layout, 1, 230, "PRIMEIRA PESSOA", "Entre no mundo pelos olhos de Kin.", UiIcon.FirstPerson);
            Text hint = ui.Label("Menu Guide Hint", "Conheça Kin e os poderes do jardim no GUIA.", layout, 20, RuntimeUiFactory.Paper, new Vector2(920, 44), TextAnchor.MiddleCenter);
            hint.fontStyle = FontStyle.Normal; hint.rectTransform.anchoredPosition = new Vector2(0, -300);
            RectTransform footer = ui.Rect("Menu Navigation", home); footer.sizeDelta = new Vector2(932, 68);
            footer.anchorMin = footer.anchorMax = footer.pivot = new Vector2(.5f, 0); footer.anchoredPosition = new Vector2(0, 28);
            RuntimeUiFactory.Fit(footer, new Vector2(120, 0));
            string[] names = { "HISTÓRIA", "CONFIGURAÇÕES", "CRÉDITOS", "SAIR" };
            for (int i = 0; i < names.Length; i++)
            {
                int item = i;
                Button button = ui.Button(names[i], names[i], footer, RuntimeUiFactory.Ink, new Vector2(214, 62));
                ((RectTransform)button.transform).anchoredPosition = new Vector2((i - 1.5f) * 236, 0);
                button.onClick.AddListener(() => { if (GuideBlocksInput) return; if (item == 3) QuitGame();
                    else SetPanel(item == 0 ? Panel.Story : item == 1 ? Panel.Settings : Panel.Credits); });
            }
        }

        private void AddModeCard(Transform parent, int mode, float x, string title, string caption, UiIcon icon)
        {
            Button button = ui.Button("View " + mode, "", parent, mode == 0 ? RuntimeUiFactory.Jade : RuntimeUiFactory.Ink, new Vector2(438, 230));
            RectTransform rect = (RectTransform)button.transform; rect.anchoredPosition = new Vector2(x, -131);
            ui.Icon("View Symbol", icon, rect, new Vector2(68, 68), RuntimeUiFactory.Gold).rectTransform.anchoredPosition = new Vector2(-155, 41);
            Text label = button.GetComponentInChildren<Text>(); label.text = title; label.fontSize = label.resizeTextMaxSize = 25;
            label.rectTransform.sizeDelta = new Vector2(400, 48); label.rectTransform.anchoredPosition = new Vector2(0, -18);
            Text description = ui.Label("View Description", caption, rect, 21, RuntimeUiFactory.Paper, new Vector2(380, 64), TextAnchor.MiddleCenter);
            description.fontStyle = FontStyle.Normal; description.rectTransform.anchoredPosition = new Vector2(0, -73);
            Text play = ui.Label("View Play", "JOGAR", rect, 18, RuntimeUiFactory.Gold, new Vector2(110, 34), TextAnchor.MiddleRight);
            play.rectTransform.anchoredPosition = new Vector2(124, 48);
            button.onClick.AddListener(() => BeginIntro(mode));
        }

        private void BuildPanels()
        {
            modal = ui.Panel("Menu Modal Backdrop", safeArea, RuntimeUiFactory.Veil, Vector2.zero);
            RuntimeUiFactory.Stretch(modal, Vector2.zero);
            RectTransform card = ui.Surface("Menu Book", modal, RuntimeUiFactory.Paper, new Vector2(960, 690), true);
            RuntimeUiFactory.Fit(card, new Vector2(80, 100));
            credits = BuildInfo(card, "CRÉDITOS", "UM JOGO DE ARIEL KÜHN QUINT", "Criação, game design e programação\nAriel Kühn Quint\n\nProdução independente\nQuint / QiP Games");
            story = BuildInfo(card, "A HISTÓRIA DE KIN", "QUANDO A CASA DORME, ELE DESPERTA", "Demônios avançam por caminhos encantados em direção à casa. Kin, o gato samurai, é o guardião deste jardim.\n\nConstrua defesas, domine os portais e lute ao lado das lanternas. Cada noite é uma nova chance de proteger seu lar.");
            RectTransform options = ui.Rect("Settings Content", card); RuntimeUiFactory.Stretch(options, Vector2.zero); settings = options.gameObject;
            Text heading = ui.Label("Settings Title", "CONFIGURAÇÕES", options, 36, RuntimeUiFactory.Ink, new Vector2(800, 64), TextAnchor.MiddleCenter);
            heading.rectTransform.anchoredPosition = new Vector2(0, 250); ui.Rule(options, 780, new Vector2(0, 192));
            Text volume = ui.Label("Volume Title", "VOLUME", options, 22, RuntimeUiFactory.Ink, new Vector2(280, 42), TextAnchor.MiddleLeft);
            volume.rectTransform.anchoredPosition = new Vector2(-250, 116);
            Text amount = ui.Label("Volume Amount", Mathf.RoundToInt(AudioListener.volume * 100) + "%", options, 22, RuntimeUiFactory.Jade, new Vector2(100, 40), TextAnchor.MiddleRight);
            amount.rectTransform.anchoredPosition = new Vector2(334, 116);
            Slider slider = ui.Slider("Master Volume", options, new Vector2(760, 54), AudioListener.volume);
            ((RectTransform)slider.transform).anchoredPosition = new Vector2(0, 59);
            slider.onValueChanged.AddListener(value => { AudioListener.volume = value; amount.text = Mathf.RoundToInt(value * 100) + "%"; });
            Button fullscreen = ui.Button("Fullscreen Setting", "", options, RuntimeUiFactory.Jade, new Vector2(760, 64));
            ((RectTransform)fullscreen.transform).anchoredPosition = new Vector2(0, -32);
            Text fullscreenText = fullscreen.GetComponentInChildren<Text>();
            fullscreenText.text = Application.isMobilePlatform ? "TELA ADAPTADA AO DISPOSITIVO" : "TELA CHEIA  ·  " + (Screen.fullScreen ? "ATIVADA" : "DESATIVADA");
            fullscreen.interactable = !Application.isMobilePlatform;
            fullscreen.onClick.AddListener(() => { Screen.fullScreen = !Screen.fullScreen;
                fullscreenText.text = "TELA CHEIA  ·  " + (Screen.fullScreen ? "ATIVADA" : "DESATIVADA"); });
            Button repeat = ui.Button("Repeat Tutorial", "REPETIR TUTORIAL", options, RuntimeUiFactory.Gold, new Vector2(760, 68));
            ((RectTransform)repeat.transform).anchoredPosition = new Vector2(0, -130);
            tutorialMessage = ui.Label("Tutorial Setting Message", "O GUIA também está disponível a qualquer momento.", options, 21, RuntimeUiFactory.Jade, new Vector2(810, 48), TextAnchor.MiddleCenter);
            tutorialMessage.fontStyle = FontStyle.Normal; tutorialMessage.rectTransform.anchoredPosition = new Vector2(0, -193);
            repeat.onClick.AddListener(() => { if (GuideBlocksInput) return; TutorialDirector.ResetTutorial(); tutorialMessage.text = "Kin vai te guiar na próxima partida."; });
            Button back = ui.Button("Close Menu Panel", "VOLTAR", card, RuntimeUiFactory.Ink, new Vector2(280, 66));
            ((RectTransform)back.transform).anchoredPosition = new Vector2(0, -280); back.onClick.AddListener(() => SetPanel(Panel.None));
            modal.gameObject.AddComponent<HudReveal>(); modal.gameObject.SetActive(false);
        }

        private GameObject BuildInfo(RectTransform parent, string title, string subtitle, string copy)
        {
            RectTransform content = ui.Rect(title, parent); RuntimeUiFactory.Stretch(content, Vector2.zero);
            Text heading = ui.Label("Panel Heading", title, content, 36, RuntimeUiFactory.Ink, new Vector2(800, 64), TextAnchor.MiddleCenter);
            heading.rectTransform.anchoredPosition = new Vector2(0, 250); ui.Rule(content, 780, new Vector2(0, 192));
            Text eyebrow = ui.Label("Panel Subtitle", subtitle, content, 20, RuntimeUiFactory.Jade, new Vector2(800, 44), TextAnchor.MiddleCenter);
            eyebrow.rectTransform.anchoredPosition = new Vector2(0, 132);
            Text body = ui.Label("Panel Text", copy, content, 27, RuntimeUiFactory.Ink, new Vector2(770, 300), TextAnchor.MiddleCenter);
            body.fontStyle = FontStyle.Normal; body.rectTransform.anchoredPosition = new Vector2(0, -52);
            return content.gameObject;
        }

        private void SetPanel(Panel value)
        {
            if (GuideBlocksInput) return;
            activePanel = value; modal.gameObject.SetActive(value != Panel.None);
            credits.SetActive(value == Panel.Credits); story.SetActive(value == Panel.Story); settings.SetActive(value == Panel.Settings);
        }

        private void BuildComic()
        {
            comic = ui.Panel("Kin Story", safeArea, RuntimeUiFactory.Ink, Vector2.zero); RuntimeUiFactory.Stretch(comic, Vector2.zero);
            RectTransform image = ui.Rect("Story Scene", comic); RuntimeUiFactory.Stretch(image, Vector2.zero);
            comicImage = image.gameObject.AddComponent<Image>(); comicImage.preserveAspect = true; comicImage.raycastTarget = false;
            RectTransform footer = ui.Surface("Story Footer", comic, RuntimeUiFactory.Ink, new Vector2(1360, 230), true);
            footer.anchorMin = footer.anchorMax = footer.pivot = new Vector2(.5f, 0); footer.anchoredPosition = new Vector2(0, 28);
            RuntimeUiFactory.Fit(footer, new Vector2(80, 0));
            comicCaption = ui.Label("Comic Caption", "", footer, 28, RuntimeUiFactory.Paper, new Vector2(1240, 100), TextAnchor.MiddleCenter);
            comicCaption.fontStyle = FontStyle.Normal; comicCaption.rectTransform.anchoredPosition = new Vector2(0, 46);
            comicProgress = ui.Label("Comic Progress", "", footer, 20, RuntimeUiFactory.Muted, new Vector2(480, 40), TextAnchor.MiddleCenter);
            comicProgress.rectTransform.anchoredPosition = new Vector2(0, -60);
            Button skip = ui.Button("Skip Story", "PULAR", footer, RuntimeUiFactory.Jade, new Vector2(200, 62));
            ((RectTransform)skip.transform).anchoredPosition = new Vector2(-534, -62);
            skip.onClick.AddListener(() => { if (!GuideBlocksInput) StartGame(pendingCameraMode); });
            comicNext = ui.Button("Next Scene", "PRÓXIMO", footer, RuntimeUiFactory.Gold, new Vector2(330, 62));
            ((RectTransform)comicNext.transform).anchoredPosition = new Vector2(470, -62); nextLabel = comicNext.GetComponentInChildren<Text>();
            comicNext.onClick.AddListener(() => { if (GuideBlocksInput) return; if (comicPanel < 4) { comicPanel++; RefreshComic(); } else StartGame(pendingCameraMode); });
            comic.gameObject.SetActive(false);
        }

        private void BeginIntro(int cameraMode)
        {
            if (GuideBlocksInput) return;
            pendingCameraMode = cameraMode; comicPanel = 0; showingIntro = comicScenes[0] != null;
            if (!showingIntro) { StartGame(cameraMode); return; }
            home.gameObject.SetActive(false); modal.gameObject.SetActive(false); comic.gameObject.SetActive(true); RefreshComic();
        }

        private void RefreshComic()
        {
            comicImage.sprite = comicSprites[comicPanel]; comicCaption.text = ComicCaptions[comicPanel];
            comicProgress.text = (comicPanel + 1) + " / 5  ·  A LENDA DO GUARDIÃO";
            nextLabel.text = comicPanel == 4 ? "DEFENDER A CASA" : "PRÓXIMO"; comicNext.GetComponent<HudMotion>().Punch();
        }

        private static Texture2D LoadStoryTexture(int sceneNumber)
        {
            TextAsset image = Resources.Load<TextAsset>($"UI/IntroScene{sceneNumber}Data");
            if (image == null) return null;
            Texture2D texture = new(2, 2, TextureFormat.RGB24, false) { name = $"IntroScene{sceneNumber}_Runtime" };
            if (ImageConversion.LoadImage(texture, image.bytes, true)) return texture;
            Object.Destroy(texture); return null;
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea) ApplySafeArea();
            inputGroup.interactable = inputGroup.blocksRaycasts = !GuideBlocksInput;
            if (!GuideBlocksInput && !showingIntro && activePanel != Panel.None && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetPanel(Panel.None);
        }
        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Mathf.Max(1, Screen.width), lastSafeArea.yMin / Mathf.Max(1, Screen.height));
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Mathf.Max(1, Screen.width), lastSafeArea.yMax / Mathf.Max(1, Screen.height));
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
        private void OnDestroy() { foreach (Object item in owned) if (item != null) Destroy(item); }
        private static void StartGame(int cameraMode)
        {
            PlayerPrefs.SetInt("CameraMode", cameraMode); PlayerPrefs.Save(); CampaignProgress.BeginNewCampaign(); SceneManager.LoadScene("Game");
        }
        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
