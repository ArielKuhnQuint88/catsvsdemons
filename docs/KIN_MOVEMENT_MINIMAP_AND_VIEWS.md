# Kin: movimento, pata de ataque, minimapa e visões

## Controles

| Ação | PC | Android |
|---|---|---|
| Andar | WASD ou setas | Joystick no canto inferior esquerdo |
| Recuar sem virar | S / seta para baixo | Puxe o joystick para trás |
| Deslizar para os lados no combate | A/D ou setas laterais | Joystick lateral |
| Trocar de visão | Botão junto do minimapa ou V | Botão junto do minimapa |
| Olhar em primeira pessoa | Segure o botão direito e arraste | Arraste um dedo no lado direito |
| Zoom/pan isométrico | Enquadramento da fase/tutorial | Dois dedos |
| Guia | GUIA ou F1 | GUIA |

O movimento tem aceleração e frenagem suaves. Recuar preserva a direção do
corpo. Durante o combate, o movimento lateral mantém Kin voltado para o alvo;
a primeira pessoa separa o olhar do deslocamento. No celular, o joystick e o
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
Mostra as rotas da fase, a casa, Kin com sua direção, demônios que acompanham
as posições reais e torres. Portal usa azul, Bonsai usa verde e Lanterna usa
dourado. Uma torre que recebe dano ganha halo vermelho pulsante; o cabeçalho
indica quantas estão sob ataque. O alerta dura 2,5 segundos após o último dano
válido. Cura e dano zero não disparam o aviso. Torres destruídas e demônios
mortos saem do mapa.

As rotas são lidas após a atualização final da fase e o mapa preserva a
proporção do terreno. O traçado só é reconstruído quando o layout muda; os
marcadores atualizam a 20 Hz. O desenho usa geometria UGUI, sem uma segunda
câmera ou RenderTexture. O mapa fica congelado com a partida e é ocultado
junto do HUD ao entrar na casa.

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

1. Feche a Unity e extraia `Cats_vs_Demons_Movimento_Minimapa_Cameras.zip`.
2. Copie `Assets` para a raiz do projeto (junto de `Packages` e
   `ProjectSettings`), substituindo os arquivos correspondentes.
3. Abra com Unity **6000.5.4f1**, aguarde a importação e inicie `MainMenu`.
4. Em Play Mode, use **Tools → Cats vs Demons → Verificar movimento câmera e
   minimapa (Play Mode)** e o diagnóstico de interface.
5. Teste recuo, diagonal, frenagem, ataque parado/em movimento, troca repetida
   de câmera, roupa equipada, torres atacadas/destruídas, pausa e guia. Confira
   a atualização dos caminhos ao passar de fase. No Android, teste dois dedos
   na primeira pessoa e na visão isométrica, além de toque nos botões.

## Verificações de desenvolvimento

- 201 verificações com o código real de movimento, animação de ossos,
  câmera, pata/katana, projeção, minimapa, registros e dano em torres.
  Incluem 30/60/120 Hz, recuo, retorno do braço, pernas durante ataque,
  conservação dos estados dos renderizadores, zoom da fase em primeira
  pessoa, gestos simultâneos no celular, botão/V, pausa/guia, marcadores
  móveis, alertas e remoção de mortos.
- 317 verificações anteriores da interface continuam passando contra os
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
