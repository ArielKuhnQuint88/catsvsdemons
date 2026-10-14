# Kin: movimento, pata de ataque, minimapa e visões

## Controles

| Ação | PC | Android |
|---|---|---|
| Andar | WASD ou setas | Joystick no canto inferior esquerdo |
| Ir para baixo no isométrico / recuar em primeira pessoa | S / seta para baixo | Puxe o joystick para baixo |
| Ir para os lados da câmera, inclusive no combate | A/D ou setas laterais | Joystick lateral |
| Trocar de visão | Botão junto do minimapa ou V | Botão junto do minimapa |
| Olhar em primeira pessoa | Segure o botão direito e arraste | Arraste um dedo no lado direito |
| Zoom/pan isométrico | Enquadramento da fase/tutorial | Dois dedos |
| Guia | GUIA ou F1 | GUIA |

O movimento tem aceleração e frenagem suaves. Na visão isométrica, todas as
direções usam a câmera: W sobe na tela, S desce e A/D seguem os lados. Essa
regra continua igual quando Kin vira para um inimigo; atacar não troca os
eixos do controle. Diagonais não ganham velocidade e a inclinação da câmera
não empurra Kin para cima/baixo do terreno. A direção do corpo e a animação
de recuo são independentes do rumo do deslocamento. No combate, Kin mantém
o olhar no alvo. Na primeira pessoa, deslocamento e recuo seguem a direção
do olhar. No celular, o joystick e o
olhar podem usar dedos diferentes ao mesmo tempo. Toques sobre a interface
não iniciam movimento ou rotação da câmera. Ao usar dois dedos na visão
isométrica, o gesto continua reservado ao zoom/pan.

O ataque automático, dano, ganho de energia, moedas e inventário conservam
suas regras. Uma pata branca com punho da roupa equipada e katana dourada
acompanha o antebraço no jardim. A versão em primeira pessoa mostra a mesma
pata e espada diante da câmera. O rig mantém as pernas em movimento durante
o golpe e zera os deslocamentos do braço ao completar a animação. O modelo
original continua disponível se o rig tiver de usar seu fallback.

## Minimapa

O mapa fica abaixo de PAUSAR/GUIA, com o botão de câmera na parte inferior.
O painel agora mede 440 × 492 unidades do Canvas; a área do desenho passou
para 400 × 304, mais que o dobro da área anterior. Caminhos mais contrastantes
e símbolos maiores distinguem Kin, casa, demônios e torres.

O zoom inicial é **2×**. **+** aproxima e **−** afasta, entre **1× e 3×**;
a escolha fica salva. Em 1×, as trilhas completas cabem no mapa. Ao aproximar,
o foco fica na casa e acompanha Kin quando ele sai da região central.
Caminhos são recortados nos limites do mapa mantendo o traçado real, sem
prender trechos inteiros na borda. Marcadores fora do recorte ficam na borda
para indicar sua direção. Pausa e guia bloqueiam os botões de zoom.
Mostra as rotas da fase, a casa, Kin com sua direção, demônios que acompanham
as posições reais e torres. Portal usa azul, Bonsai usa verde e Lanterna usa
dourado. Uma torre que recebe dano ganha halo vermelho pulsante; o cabeçalho
indica quantas estão sob ataque. O alerta dura 2,5 segundos após o último dano
válido. Cura e dano zero não disparam o aviso. Torres destruídas e demônios
mortos saem do mapa.

As rotas são lidas após a atualização final da fase e o mapa preserva a
proporção do terreno. O traçado só é reconstruído quando a fase, o zoom
ou o foco mudam; os marcadores atualizam a 20 Hz. O desenho usa geometria UGUI, sem uma segunda
câmera ou RenderTexture. O mapa fica congelado com a partida e é ocultado
junto do HUD ao entrar na casa.

## Correção da interface que desaparecia

As camadas `Map Roads` e `Map Actors` agora recebem `CanvasRenderer` antes
de adicionar/ativar o componente gráfico. A classe do minimapa também declara
essa dependência explicitamente. Isso corrige o `MissingComponentException`
mostrado no Console.

Vida, energia, torres, pausa, resultados e tutorial terminam de ser montados
antes do minimapa. Se a inicialização opcional do mapa falhar, sua camada
incompleta é removida, o erro é registrado uma vez e o HUD principal continua
ativo. V ainda pode trocar de câmera. A camada do mapa fica abaixo dos menus
de pausa/resultado, para não cobrir seus botões. O diagnóstico de interface
no Editor verifica `CanvasRenderer` em todos os elementos gráficos.

## Trocar de câmera durante a partida

PRIMEIRA PESSOA / ISOMÉTRICO alterna sem reiniciar a fase, alterar vida,
moedas ou equipamentos. O enquadramento isométrico conserva posição, zoom
manual e a escala da fase mais recente. Uma escolha de torre ainda não paga
é cancelada na troca. Pausa, fim da partida e guia bloqueiam a ação.

O próprio corpo fica oculto em primeira pessoa com `forceRenderingOff`;
`Renderer.enabled` continua pertencendo ao rig e às roupas. Ao voltar, os
estados anteriores de visibilidade são restaurados, sem reativar os modelos
provisórios. A prévia do perfil limpa essa ocultação no clone para continuar
mostrando Kin equipado. O planejamento dos caminhos usa o enquadramento
isométrico salvo, mesmo quando a câmera principal está na primeira pessoa.

## Atualizar no PC

1. Feche a Unity e extraia `Cats_vs_Demons_Minimapa_Maior_Zoom.zip`.
2. Copie `Assets` para a raiz do projeto (junto de `Packages` e
   `ProjectSettings`), substituindo os arquivos correspondentes.
3. Abra com Unity **6000.5.4f1**, aguarde a importação e inicie `MainMenu`.
4. Em Play Mode, use **Tools → Cats vs Demons → Verificar movimento câmera e
   minimapa (Play Mode)** e o diagnóstico de interface.
5. Limpe o Console para distinguir os registros anteriores. Teste +/− entre 1× e 3×. Confirme que
   `Map Roads`/`Map Actors` não geram novos erros e que vida, torres, moedas,
   pausa, guia e minimapa aparecem. Teste W/S/A/D antes e durante o combate.
6. Teste recuo, diagonal, frenagem, ataque parado/em movimento, troca repetida
   de câmera, roupa equipada, torres atacadas/destruídas, pausa e guia. Confira
   a atualização dos caminhos ao passar de fase. No Android, teste dois dedos
   na primeira pessoa e na visão isométrica, além de toque nos botões.

## Verificações de desenvolvimento

- 1042 verificações com o código real de movimento, animação de ossos,
  câmera, pata/katana, projeção, minimapa, registros e dano em torres.
  Incluem 30/60/120 Hz, recuo, retorno do braço, pernas durante ataque,
  conservação dos estados dos renderizadores, zoom da fase em primeira
  pessoa, gestos simultâneos no celular, botão/V, pausa/guia, marcadores
  móveis, alertas e remoção de mortos. Incluem as oito direções em câmeras
  e orientações diferentes, controle real antes/durante o combate, a
  exigência de CanvasRenderer ao ativar um gráfico e uma falha simulada
  do minimapa sem interromper os botões, pausa, guia ou V do HUD. O zoom
  verifica botões +/−, limites, persistência, bloqueios, foco no Kin, projeção
  conjunta de trilhas/marcadores e recorte de segmentos paralelos/diagonais.
- A versão anterior do minimapa reproduziu o erro `CanvasRenderer` em
  `Map Roads` com o verificador mais estrito; a versão corrigida passou.
- 337 verificações da interface continuam passando contra os
  novos componentes, em cinco proporções/áreas seguras de tela.
- As checagens usam substitutos mínimos da API Unity e matemática .NET;
  não executam renderização, física ou entrada física do motor.

**Unity não está instalada neste ambiente.** Importação/compilação nativa,
skinning final, colisões, visual e sensação do movimento na Game View,
entrada em aparelho, FPS e AAB assinado precisam ser validados no PC.

As prévias em `Previas` foram renderizadas fora da Unity. A pata usa a
geometria gerada pelo código; os layouts usam os sprites e a geometria UGUI
reais com fonte aproximada. Os caminhos/demônios do minimapa nas prévias são
exemplos do verificador, e o fundo do HUD é liso. Não são capturas da Game View.
