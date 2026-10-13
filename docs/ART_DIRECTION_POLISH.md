# Jardim e apresentação de Cats vs Demons

## O que muda

Os caminhos deixam de ser cubos separados: cada rota passa a usar um piso
contínuo de pedra, com borda, curvas suavizadas e collider. Os mesmos pontos
alimentam a navegação dos demônios. As juntas antigas são substituídas por
pontos ordenados, e a pavimentação usa coordenadas do mundo para manter o
mesmo desenho nos encontros entre rotas. O terreno permanece abaixo das
bordas, inclusive nas entradas que se prolongam fora da câmera.

O tabuleiro retangular e os objetos antigos dão lugar a um terreno amplo com
textura de musgo, relevo suave fora da área de combate e três camadas de
montanhas. Céu, neblina, iluminação e cenário usam a mesma paleta: verdes
jade e sálvia, pedra quente, madeira, telhados azulados, rosa suave e dourado.
As três regiões e as quatro estações têm variações dessa paleta.

O jardim inclui uma casa com telas shoji, degraus, telhados curvos e lanternas;
pavilhões, uma pagoda, portões torii, muros baixos, lagos com carpas e pontes.
Pinheiros, cerejeiras, árvores de folhas sazonais, bambus, arbustos,
samambaias, flores e pedras são distribuídos com distância das rotas e dos
pontos de construção. A vegetação recebe uma brisa discreta; telhados e
madeira têm detalhes de superfície. Os novos elementos decorativos não
introduzem obstáculos físicos no combate.

O HUD de vida, energia e moedas ocupa o canto superior esquerdo. A seleção
de defesas fica numa faixa inferior, com cores harmonizadas e as animações
existentes. O tutorial sobe para deixar os botões livres e mantém o retrato
de Kin, as falas, os marcadores e o zoom. A visão isométrica abre um pouco o
campo para incluir o fundo; o gesto de câmera continua disponível no celular.
Os materiais das torres construídas e dos personagens são preservados nas
trocas de estação.

## Aplicar no PC

1. Feche a Unity e extraia `Cats_vs_Demons_Atualizacao_Visual.zip`.
2. Copie a pasta `Assets` do ZIP para a raiz do projeto — a pasta que já tem
   `Assets`, `Packages` e `ProjectSettings` — e aceite substituir os arquivos.
3. Abra o projeto no Unity Hub com Unity **6000.5.4f1**, aguarde a importação
   e inicie uma partida. O cenário é montado ao entrar no jogo, em Play Mode.

Não precisa posicionar os novos objetos no Editor. O arquivo ZIP contém
também as correções anteriores de personagens, shaders, tutorial, HUD e
exportador, para manter essa atualização consistente. Preserve uma cópia
do seu projeto antes de substituir arquivos se tiver alterações locais próprias.

Se preferir atualizar pelo Git, a revisão está na branch
`feature/android-release-polish`. Ela ainda está num pull request em rascunho;
baixar apenas `main` não recebe estas mudanças.

## Conferir na Unity

- Em Play Mode, execute **Tools > Cats vs Demons > Verificar cenário e caminhos
  (Play Mode)**. O diagnóstico confere juntas, malha de piso, collider,
  ocultação dos segmentos antigos e erros conhecidos dos shaders.
- Confira os caminhos, a casa e Kin na Game View. Percorra o mapa, construa
  cada defesa e observe demônios fazendo curvas até a casa.
- Teste as fases 1–12, em especial 4 → 5 e 8 → 9. Confira a troca do cenário,
  a continuidade das trilhas e as torres que permanecem entre ondas.
- Reative o guia em **Configurações → REPETIR TUTORIAL** e confira a faixa
  inferior, os marcadores e o zoom. Teste pausa, loja e retorno à partida.
- Em Android, confira safe area, joystick, botões e câmera. Meça FPS e memória
  num aparelho real antes de exportar a versão para publicação.

O preparo/exportador Android inclui os shaders `GardenScenery`, `GardenWater`
e `Skybox/Procedural` e verifica a textura necessária. O restante do fluxo
continua no [guia do Play Console](PLAY_CONSOLE_RELEASE.md).

## Verificações de desenvolvimento

A geometria real dos três novos componentes foi compilada e executada em C#
com substitutos mínimos da API Unity. As 24 combinações de região, estação e
perfil PC/celular verificam vértices, índices, normais, cores, UVs, orientação
das faces, continuidade das rotas, limites dos cantos e terreno sob as bordas.
O cenário está agrupado em 18 malhas: aproximadamente 37–39 mil triângulos no
PC e 27–29 mil no celular, além dos caminhos, personagens, torres e efeitos.
Isso é uma contagem de geometria, não uma medição de FPS.

O HLSL de `GardenScenery` passou por 16 verificações de estágio/condicional
em glslang e validação de SPIR-V usando substitutos mínimos dos includes URP.
Sintaxe dos arquivos C#, metadados Unity e conteúdo do ZIP foram conferidos.
Uma prévia de composição foi renderizada fora da Unity com as malhas geradas
e os modelos originais. Ela não reproduz exatamente o shader, o céu, a
iluminação, o pós-processamento ou o HUD do motor.

**Unity não está instalada neste ambiente.** Compilação com os includes reais,
Game View, física, FPS em dispositivo e AAB assinado continuam pendentes.

## Textura criada para o terreno

- Modo: geração de uma imagem nova com imagegen, sem imagem de referência.
- Asset: `Assets/_Project/Resources/Textures/Environment/PaintedMoss.jpg`.
- A geração resultou em um PNG RGB de 1254 × 1254. O asset foi convertido para
  JPEG de qualidade 88, mantendo composição e dimensões. O importador usa
  repetição, mipmaps, limite de 1024 e compressão ASTC no Android.
- Prompt: square seamless top-down hand-painted moss and short-grass texture
  for a Japanese isometric game; muted jade, sage and olive earthy greens,
  tiny soil flecks and small moss patches; diffuse light, no baked shadows,
  no perspective, horizon, trees, paths, big stones, text, logo, border or
  transparency; low contrast, balanced detail, fill the frame and tile on
  both axes; requested 1024 × 1024.

As cores dos vértices são convertidas para o espaço linear quando necessário,
como descrito em [Color.linear](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Color-linear.html).
