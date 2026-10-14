using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    /// <summary>Scene-owned reference guide, available independently of the battle or shop HUD.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class KinGuideOverlay : MonoBehaviour
    {
        private static KinGuideOverlay instance;
        private RectTransform safeArea;
        private GameObject overlay;
        private RectTransform portrait;
        private Sprite portraitSprite;
        private Text heading, dialogue, objective, pageCount;
        private readonly Image[] tabs = new Image[6];
        private Button previous, next, practice;
        private Rect lastSafeArea;
        private bool open;
        private int page;
        private int consumedFrame = -1;
        private float resumeScale;

        public static bool IsOpen => instance != null && instance.open;
        public static bool ConsumedInputThisFrame => instance != null && instance.consumedFrame == Time.frameCount;

        private static readonly string[] Topics = { "ANDAR", "CONSTRUIR", "PODERES", "COMBATE", "LOJA", "CÂMERA" };
        private static readonly string[] Titles =
        {
            "VAMOS CONHECER O JARDIM", "ESCOLHA, DEPOIS CONSTRUA", "PERTO DE MIM, ELAS FICAM MAIS FORTES",
            "EU CUIDO DA ESPADA; VOCÊ CUIDA DO CAMINHO", "UMA PAUSA PARA NOS PREPARAR", "ENCONTRE O MELHOR ÂNGULO"
        };

        public static KinGuideOverlay Ensure()
        {
            if (instance != null) return instance;
            instance = Object.FindFirstObjectByType<KinGuideOverlay>();
            if (instance == null) instance = new GameObject("Kin Guide Access").AddComponent<KinGuideOverlay>();
            return instance;
        }

        public static void OpenGuide(int topic = 0) => Ensure().Open(topic);

        private void Awake()
        {
            instance = this;
            RuntimeUiFactory ui = new();
            Canvas canvas = ui.CreateCanvas(transform);
            canvas.name = "Kin Guide Canvas";
            canvas.sortingOrder = 950;
            safeArea = ui.Rect("Safe Area", canvas.transform);
            safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            Button access = ui.Button("Guide Anytime", "GUIA", safeArea, RuntimeUiFactory.Ink, new Vector2(176, 62));
            RectTransform accessRect = (RectTransform)access.transform;
            accessRect.anchorMin = accessRect.anchorMax = accessRect.pivot = new Vector2(1, 1);
            accessRect.anchoredPosition = new Vector2(-30, -106);
            Text accessLabel = access.GetComponentInChildren<Text>(); accessLabel.color = RuntimeUiFactory.Paper;
            accessLabel.rectTransform.sizeDelta = new Vector2(115, 46); accessLabel.rectTransform.anchoredPosition = new Vector2(20, 0);
            accessLabel.fontSize = accessLabel.resizeTextMaxSize = 18;
            ui.Icon("Guide Symbol", UiIcon.Book, accessRect, new Vector2(29, 29), RuntimeUiFactory.Gold).rectTransform.anchoredPosition = new Vector2(-53, 0);
            access.onClick.AddListener(() => Open(0));

            RectTransform shade = ui.Panel("Guide Backdrop", safeArea, RuntimeUiFactory.Veil, Vector2.zero);
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            overlay = shade.gameObject;
            RectTransform card = ui.Surface("Kin Guide Book", shade, RuntimeUiFactory.Paper, new Vector2(1130, 700), true);
            RuntimeUiFactory.Fit(card, new Vector2(80, 100));
            overlay.AddComponent<HudReveal>(); ui.Rule(card, 1030, new Vector2(0, 218));
            heading = ui.Label("Guide Title", "", card, 30, RuntimeUiFactory.Ink,
                new Vector2(890, 64), TextAnchor.MiddleLeft);
            ((RectTransform)heading.transform).anchoredPosition = new Vector2(-74, 279);
            Button close = ui.Button("Close Guide", "FECHAR", card, RuntimeUiFactory.Ink, new Vector2(142, 56));
            ((RectTransform)close.transform).anchoredPosition = new Vector2(463, 279);
            close.onClick.AddListener(Close);

            for (int i = 0; i < Topics.Length; i++)
            {
                int topic = i;
                Button tab = ui.Button("Guide " + Topics[i], Topics[i], card, RuntimeUiFactory.Ink,
                    new Vector2(162, 60));
                ((RectTransform)tab.transform).anchoredPosition = new Vector2(-447 + i * 179, 177);
                Text tabLabel = tab.GetComponentInChildren<Text>(); tabLabel.fontSize = tabLabel.resizeTextMaxSize = 18;
                tabs[i] = tab.GetComponent<Image>();
                tab.onClick.AddListener(() => SetPage(topic));
            }
            portrait = ui.Rect("Kin Speaking", card);
            portrait.sizeDelta = new Vector2(228, 254);
            portrait.anchoredPosition = new Vector2(-404, -5);
            Image avatar = portrait.gameObject.AddComponent<Image>();
            Texture2D texture = Resources.Load<Texture2D>("UI/KinTutorialPortrait");
            if (texture != null)
            {
                portraitSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                avatar.sprite = portraitSprite;
            }
            avatar.preserveAspect = true; avatar.raycastTarget = false;
            Text name = ui.Label("Speaker", "KIN • SEU GUIA", card, 22, RuntimeUiFactory.Ink, new Vector2(250, 38), TextAnchor.MiddleCenter);
            ((RectTransform)name.transform).anchoredPosition = new Vector2(-404, -153);
            dialogue = ui.Label("Guide Dialogue", "", card, 25, RuntimeUiFactory.Ink,
                new Vector2(748, 238), TextAnchor.UpperLeft);
            ((RectTransform)dialogue.transform).anchoredPosition = new Vector2(145, -10);
            dialogue.fontStyle = FontStyle.Normal;
            objective = ui.Label("Guide Action", "", card, 23, RuntimeUiFactory.Jade,
                new Vector2(1040, 72), TextAnchor.MiddleCenter);
            ((RectTransform)objective.transform).anchoredPosition = new Vector2(0, -221);
            pageCount = ui.Label("Guide Page", "", card, 18, RuntimeUiFactory.Ink, new Vector2(300, 34), TextAnchor.MiddleCenter);
            ((RectTransform)pageCount.transform).anchoredPosition = new Vector2(0, -306);
            practice = ui.Button("Practice Tutorial", "PRATICAR NO JARDIM", card, RuntimeUiFactory.Gold, new Vector2(282, 52));
            ((RectTransform)practice.transform).anchoredPosition = new Vector2(0, -299);
            practice.GetComponentInChildren<Text>().color = RuntimeUiFactory.Ink;
            practice.onClick.AddListener(() =>
            {
                ResponsiveCanvasHud hud = Object.FindFirstObjectByType<ResponsiveCanvasHud>();
                if (hud == null || !hud.CanReplayTutorial) return;
                Close();
                hud.GetComponent<TutorialDirector>()?.Replay();
            });
            previous = ui.Button("Previous Lesson", "VOLTAR", card, RuntimeUiFactory.Ink, new Vector2(230, 58));
            ((RectTransform)previous.transform).anchoredPosition = new Vector2(-381, -299);
            previous.onClick.AddListener(() => SetPage(page - 1));
            next = ui.Button("Next Lesson", "PRÓXIMO", card, RuntimeUiFactory.Ink, new Vector2(230, 58));
            ((RectTransform)next.transform).anchoredPosition = new Vector2(381, -299);
            next.onClick.AddListener(() => { if (page == 5) Close(); else SetPage(page + 1); });
            overlay.SetActive(false);
            ApplySafeArea();
        }

        private void Open(int topic)
        {
            if (!open) { resumeScale = Time.timeScale; Time.timeScale = 0f; }
            open = true;
            consumedFrame = Time.frameCount;
            overlay.SetActive(true);
            ResponsiveCanvasHud hud = Object.FindFirstObjectByType<ResponsiveCanvasHud>();
            bool canPractice = hud != null && hud.CanReplayTutorial;
            practice.gameObject.SetActive(canPractice);
            pageCount.gameObject.SetActive(!canPractice);
            SetPage(topic);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            consumedFrame = Time.frameCount;
            overlay.SetActive(false);
            Time.timeScale = resumeScale;
        }

        private void SetPage(int topic)
        {
            page = Mathf.Clamp(topic, 0, 5);
            heading.text = Titles[page];
            switch (page)
            {
                case 0:
                    dialogue.text = "Sou Kin, o guardião desta casa! Leve-me pelo jardim e fique de olho nas três trilhas. Os demônios chegam por elas.\n\n" +
                        (Application.isMobilePlatform ? "Use o joystick na região inferior esquerda. Na visão isométrica, as direções seguem a câmera, inclusive durante o combate." : "Use WASD ou as setas. Na visão isométrica, W vai para cima, S para baixo e A/D para os lados da tela. As direções continuam iguais durante o combate.") +
                        "\n\nAbra este GUIA quando precisar. A partida fica pausada enquanto conversamos.";
                    objective.text = "Feche o guia e experimente se mover pelo jardim.";
                    break;
                case 1:
                    dialogue.text = "1. Escolha PORTAL, BONSAI ou LANTERNA na faixa inferior. A defesa escolhida ganha uma borda dourada.\n\n" +
                        "2. Clique ou toque numa base iluminada. Você só paga quando a defesa é construída.\n\n" +
                        "Confira o preço e suas moedas. Se faltar dinheiro, derrote demônios. CANCELAR sai da escolha sem gastar. O minimapa marca as torres e pisca em vermelho quando recebem dano.";
                    objective.text = "Escolha uma defesa e procure os anéis nas bases livres.";
                    break;
                case 2:
                    dialogue.text = "LANTERNA • Desacelera demônios próximos. Quando estou por perto, ela lança fogo.\n\n" +
                        "BONSAI • Recupera minha vida. Perto de mim, também ajuda a reparar defesas.\n\n" +
                        "PORTAL • Construa pelo menos dois e aproxime-me de um deles para viajar até o próximo.\n\nAs torres têm vida própria: proteja-as!";
                    objective.text = "Aproxime Kin da defesa que precisa de apoio.";
                    break;
                case 3:
                    dialogue.text = "Eu ataco automaticamente quando me aproximo de um demônio. Cada golpe carrega a barra de energia.\n\n" +
                        "Quando ela fica cheia, o botão GOLPE PRONTO brilha. " +
                        (Application.isMobilePlatform ? "Toque nele para atingir os inimigos próximos." : "Clique nele ou pressione Espaço para atingir os inimigos próximos.") +
                        "\n\nProteja a casa central. Cada demônio derrotado rende moedas para nossas defesas.";
                    objective.text = "Fique perto dos inimigos e guarde o golpe para um grupo.";
                    break;
                case 4:
                    dialogue.text = "O botão PAUSAR leva você à minha casa. Entre as fases também voltamos para lá.\n\n" +
                        "Toque no computador para abrir a LOJA: roupas, acessórios e melhorias usam as moedas da partida.\n\n" +
                        "Confira meu equipamento no perfil e use CONTINUAR ou PRÓXIMA FASE para voltar ao jardim. Este guia também funciona dentro da loja.";
                    objective.text = "Na casa, procure o computador com ABRIR LOJA.";
                    break;
                default:
                    dialogue.text = (Application.isMobilePlatform
                        ? "Use dois dedos para aproximar, afastar ou deslocar a visão isométrica. O botão junto do minimapa troca para PRIMEIRA PESSOA. Nesse modo, use o joystick à esquerda e arraste no lado direito para olhar."
                        : "Veja Kin e o jardim na visão isométrica. Ao lado do minimapa, o botão PRIMEIRA PESSOA/ISOMÉTRICO troca a visão; V também alterna. Na primeira pessoa, arraste com o botão direito para olhar. Minha pata e espada aparecem ao atacar.") +
                        "\n\nNo minimapa, + aproxima e − afasta. Em 1× você vê as trilhas completas; em 2× ou 3× fica mais fácil acompanhar Kin e as torres.\n\nNo tutorial inicial, a câmera destaca a ação e as setas indicam o alvo. GUIA fica disponível o tempo todo; F1 abre ou fecha no PC.";
                    objective.text = "Pode voltar a qualquer capítulo. Vamos proteger a casa!";
                    break;
            }
            previous.interactable = page > 0;
            next.GetComponentInChildren<Text>().text = page == 5 ? "VOLTAR AO JOGO" : "PRÓXIMO";
            pageCount.text = (page + 1) + "/6 • GUIA DO KIN";
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].color = i == page ? RuntimeUiFactory.Gold : RuntimeUiFactory.Ink;
                tabs[i].GetComponentInChildren<Text>().color = i == page ? RuntimeUiFactory.Ink : RuntimeUiFactory.Paper;
                tabs[i].GetComponent<HudMotion>().SetSelected(i == page);
            }
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea) ApplySafeArea();
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame) { if (open) Close(); else Open(0); }
                else if (open && keyboard.escapeKey.wasPressedThisFrame) Close();
            }
            if (open) portrait.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.012f);
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && open) resumeScale = 0f;
        }

        private void OnDestroy()
        {
            if (open) Time.timeScale = resumeScale;
            if (portraitSprite != null) Destroy(portraitSprite);
            if (instance == this) instance = null;
        }
    }
}
