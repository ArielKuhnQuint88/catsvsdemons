# Criar o primeiro greybox na Unity

Este passo cria automaticamente:

- cena `Game`;
- terreno provisório;
- casa central provisória;
- câmera ortográfica isométrica;
- iluminação;
- organização inicial da Hierarchy;
- materiais provisórios.

## Atualizar o projeto

1. Feche a Unity ou aguarde a compilação terminar.
2. No GitHub Desktop, clique em **Fetch origin**.
3. Quando aparecer, clique em **Pull origin**.
4. Volte para a Unity e aguarde a importação dos arquivos.

## Gerar a cena

Na barra superior da Unity:

1. Abra **Tools**.
2. Abra **Cats vs Demons**.
3. Clique em **Create Greybox Scene**.
4. Confirme o salvamento da cena atual, se solicitado.

A cena será criada em:

```text
Assets/_Project/Scenes/Game.unity
```

## Resultado esperado

A Hierarchy terá:

```text
Game
├── Environment
│   ├── Board
│   ├── House
│   └── Directional Light
├── Paths
├── BuildSpots
├── Enemies
├── Player
├── Systems
└── Main Camera
```

O terreno aparece verde, a casa vermelha e a câmera mostra todo o tabuleiro em visão isométrica.

## Depois de gerar

1. Pressione `Ctrl + S`.
2. Abra o GitHub Desktop.
3. Faça um commit com:

```text
feat: generate initial greybox scene
```

4. Clique em **Push origin**.
