# Guia acessível, superfícies do Kin e construção

## Acessar o tutorial durante o jogo

O botão **GUIA ?** fica disponível no menu, na partida, na pausa, na casa e na
loja. No PC, **F1** também abre ou fecha o guia. Ele tem seis capítulos com o
retrato de Kin: movimento, construção, poderes das torres, combate, loja e câmera.

O guia usa um Canvas próprio, acima das interfaces de combate e da loja. Ao
abrir, registra o estado de pausa e interrompe a partida. Ao fechar, restaura
esse estado: um jogo em andamento continua, e uma loja ou pausa já aberta
permanece pausada. O clique ou tecla de fechamento é consumido nesse frame
para impedir construção ou gasto de energia por trás da janela.

No jardim, **PRATICAR NO JARDIM** repete as seis etapas interativas, com falas,
marcadores e zoom. Isso preserva a fase e os recursos; a repetição não concede
moedas extras. O guia de consulta continua acessível na loja e no menu. Para
rearmar o tutorial inicial da próxima partida, continua existindo
**Configurações → REPETIR TUTORIAL**.

## Aparência do Kin

O OBJ tem quatro superfícies: pelo, roupa vermelha, tecido azul esverdeado e
partes escuras. O código passa a resolver essa paleta pelo nome da superfície,
com recuperação pela ordem das submalhas quando faltam referências de material.
A importação de Kin usa materiais no modo Standard, mantendo os nomes do MTL.

Foi criada uma textura de detalhe de fibras, aplicada como variação discreta
sobre as cores do pelo e da roupa. Isso preserva a separação das quatro cores
e permite trocar roupas na loja. Uma importação reduzida a uma superfície
continua com o fallback por altura; aplicar equipamento mantém a região da
cabeça clara. Kin recebe também um mínimo de luz ambiente para manter a leitura
nas sombras.

O diagnóstico **Tools → Cats vs Demons → Verificar personagens (Play Mode)**
agora confere também UVs e a presença da textura de detalhe. Uma importação
com menos de quatro superfícies recebe aviso para reimportar
`Assets/_Project/Resources/Models/Kin.obj`.

## Construção evidente

1. Escolha **Portal**, **Bonsai** ou **Lanterna** na faixa inferior. No PC,
   os atalhos são **1**, **2** e **3**, respectivamente.
2. A escolha ganha moldura dourada, indicação visual e descrição do poder.
   Anéis pulsantes aparecem nas bases livres; no PC, a base sob o cursor fica
   mais clara. O preço mostra **MOEDAS** ou **FALTAM**, conforme seu saldo.
3. Clique ou toque numa base iluminada. A moeda é cobrada somente ao construir.
   Um pulso verde e uma mensagem confirmam o resultado. Bases ocupadas e saldo
   insuficiente recebem aviso visível.
4. **CANCELAR**, **Esc** ou o botão direito do mouse encerram a escolha sem
   gastar. Uma construção bem-sucedida também encerra a escolha; selecione
   novamente para construir outra defesa.

O toque nas bases da região inferior esquerda funciona durante a escolha de
construção. A entrada passa pelo Input System e respeita a interface, evitando
que um clique em botão compre uma torre que esteja por trás do HUD. Pressões
duplicadas no mesmo frame são ignoradas. Durante o guia e a pausa, construção,
golpe especial, ataques e efeitos automáticos de torres não avançam.

## Detalhes do jardim

Foram acrescentados canteiros de flores, folhas e flores nos lagos, faixas
penduradas nos pavilhões e acabamento nas bordas do telhado da casa. Esses
detalhes entram nas malhas existentes; o cenário continua agrupado em 18
malhas, com aproximadamente 39–41 mil triângulos no PC e 28–30 mil no celular,
além de personagens, caminhos, torres, efeitos e anéis de construção.

## Atualizar e conferir

Feche a Unity, extraia `Cats_vs_Demons_Guia_Kin_Torres.zip` e copie sua pasta
`Assets` para a raiz do projeto, substituindo os arquivos correspondentes.
Reabra com Unity **6000.5.4f1** e aguarde a importação. O pacote inclui as
correções e o cenário da revisão anterior.

- Abra **GUIA ?** durante combate, pausa e loja. Feche e confira a retomada
  correta, sem compra nem golpe acidental. Teste também F1 e Esc no PC.
- Use **PRATICAR NO JARDIM** após concluir o tutorial inicial. Confira falas,
  retrato, objetivos, câmera e conservação das moedas.
- Veja Kin na visão isométrica e no perfil da loja. Troque roupas, confira
  pelo claro, roupa com cor própria e detalhes nas superfícies.
- Escolha as três torres; teste saldo insuficiente, base ocupada, cancelar,
  confirmação de construção e dois portais conectados.
- No celular, teste toque numa base da parte inferior esquerda e a transição
  entre joystick e escolha de construção. Confira safe area e legibilidade.

## Verificações realizadas

- 39 checagens C# usando as classes reais de construção, preço e paleta, e os
  métodos reais de pausa do guia extraídos do arquivo fonte. Incluem cobrança
  única, saldo insuficiente, base ocupada, cancelamento, entrada consumida,
  pausa existente e retorno após minimizar. Usam substitutos mínimos da API Unity.
- 24 combinações de cenário/estação/perfil executadas com as classes reais de
  geometria e substitutos Unity: 2.596 assertivas, preservando orçamento,
  orientação das faces, rotas e terreno sob as bordas.
- 16 combinações de estágio/condicional HLSL do shader de personagens com
  glslang e validação de SPIR-V, usando substitutos dos includes URP.
- Sintaxe C#, metadados e integridade do ZIP conferidos.

**Unity não está instalada neste ambiente.** A compilação com includes reais,
importação nativa dos materiais, aparência na Game View, interação com mouse e
toque, física, FPS em aparelho e AAB assinado ainda precisam ser validados no PC.

## Textura de superfície

- Modo: ferramenta imagegen integrada; geração nova sem referência.
- Asset: `Assets/_Project/Resources/Textures/Characters/Kin_SurfaceDetail.jpg`.
- Origem: PNG RGB de 1254 × 1254, convertido para JPEG de qualidade 90 mantendo
  a composição e as dimensões. Importador com repetição, mipmaps, limite 1024
  e compressão ASTC para Android.
- Prompt: “Use case: stylized-concept. Asset type: seamless grayscale
  surface-detail texture for a stylized 3D white samurai cat game character.
  Primary request: one square tile of very fine short soft fur and cloth-like
  microfibres, evenly distributed across the entire image, subtle hand-painted
  detail suitable for multiplying over colored material tints. Neutral warm
  gray only, average medium-light gray, restrained contrast, fine delicate
  slightly curved fibre strokes in gently varying directions, high-frequency
  detail with no clumps or large forms. Flat diffuse lighting, no cast shadows
  or bright directional highlights, no depth perspective. The texture must
  tile seamlessly on all four sides. No character, face, clothing garment,
  objects, seams, horizon, text, logo, border, checkerboard or transparency.
  Requested 1024x1024.”

Referência da API utilizada na importação:
[ModelImporterMaterialImportMode.ImportStandard](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporterMaterialImportMode.ImportStandard.html).
