# Interface do jardim

## Identidade visual

Menu inicial, história, créditos, configurações, pausa, resultado, guia,
tutorial, casa e loja usam a mesma paleta: jade escuro, marfim, dourado suave
e coral nos avisos. Bordas finas, cantos suavizados, pequenos selos e sombras
discretas substituem molduras grossas e fundos com cores desconectadas.

A tela inicial agora usa Canvas e controles nativos, com duas escolhas de
câmera ilustradas por símbolos: visão isométrica e primeira pessoa. História,
créditos e configurações abrem em uma janela central. O volume tem uma área
de toque na faixa inteira; o modo de tela e a repetição do tutorial ficam
em controles separados. A introdução mantém os cinco desenhos, a legenda,
o progresso e as ações de avançar/pular.

Os ícones e contornos são sprites pequenos, antialiasados e gerados pelo
código. Eles são reutilizados entre as interfaces; nenhuma imagem é criada
por frame. As artes existentes de abertura, Kin, torres e equipamentos
continuam presentes.

## HUD e construção

O painel de vida/energia/moedas ficou menor, em jade, no canto esquerdo.
Pausa e guia têm símbolos próprios; o golpe especial tem uma espada, a
porcentagem de energia, aro de carga e indicação de disponibilidade.
Botões respondem ao cursor, toque e teclado com escala e mudança de tom.
Anúncios, moedas, barras de vida e abertura das janelas mantêm suas animações.

A seleção de torres não tem fundo geral, cartões retangulares cinzas,
cabeçalho nem descrição fixa acima dos botões. Mostra três artes circulares
maiores, nomes e pequenos preços com ícone de moeda. Um aro dourado e um selo
marcam a defesa escolhida. Quando faltam moedas, a arte perde intensidade e
o preço informa **FALTAM**. Selecionar ainda mostra um aviso breve para
procurar uma base iluminada; esse aviso fica no alto da tela, fora do seletor.

**CANCELAR** aparece durante a escolha. Em telas mais estreitas, muda de
posição para evitar o golpe especial. Construção, preço e teclas **1/2/3**,
**Esc** e botão direito conservam o comportamento anterior. Selecionar ou
cancelar não cobra moedas.

## Casa, loja e guia

O perfil e os botões da casa usam o mesmo acabamento do jardim. As categorias
da loja indicam a página ativa; o item escolhido ganha contorno dourado e
estado de equipamento visível. O aviso da loja tem seu próprio espaço entre
as categorias e o catálogo, sem cobrir o desenho. As roupas e acessórios
continuam usando o inventário salvo e os modelos da revisão anterior.

O **GUIA** continua disponível em qualquer tela; **F1** abre/fecha no PC.
O livro tem o retrato de Kin, seis capítulos e navegação destacada. Abrir e
fechar preserva a pausa anterior e consome o clique de fechamento. Na partida,
**PRATICAR NO JARDIM** repete o tutorial interativo. No menu,
**Configurações → REPETIR TUTORIAL** rearma o guia da próxima partida.

As janelas se ajustam à área segura real da tela. A pausa e o resultado têm
cartões próprios; o resultado também oferece retorno ao menu principal.
O joystick usa jade, marfim e um aro dourado e fica oculto durante a pausa.

## Aplicar e conferir no PC

1. Feche a Unity e extraia `Cats_vs_Demons_Interface_Renovada.zip`.
2. Copie a pasta `Assets` para a raiz do projeto (onde também estão `Packages`
   e `ProjectSettings`), aceitando substituir os arquivos correspondentes.
   Preserve suas alterações locais antes de substituir arquivos próprios.
3. Abra com Unity **6000.5.4f1**, aguarde a importação e abra `MainMenu`.
4. Em Play Mode, use **Tools → Cats vs Demons → Verificar interface (Play Mode)**.
   Confira também a leitura e as interações na Game View.
5. Teste as duas câmeras, cinco cenas da introdução, história, créditos,
   volume, tela cheia, repetição do tutorial, guia durante combate e loja,
   seleção das três torres, saldo insuficiente, cancelar, golpe, pausa,
   categorias da loja, compra/equipamento e retorno à partida.
6. No Android, confira toque na faixa inteira do volume, safe area,
   legibilidade, joystick e distância entre os botões em aparelho real.

## Evidência e limites

O código dos construtores, sprites e ações reais da interface foi compilado
e executado com substitutos mínimos da API Unity. As verificações cobrem
cinco proporções/áreas seguras de tela, limites dos controles, navegação,
volume, tutorial, capítulos da história, seleção/cancelamento das torres,
falta de moedas, pausa e bloqueio do clique de fechamento do guia. O catálogo
usa os métodos reais extraídos do controlador da casa; câmera, física e
RenderTexture do perfil não são simulados por esse verificador.

`Previas/Interface_Layout_Preview.png` foi renderizada fora da Unity a partir
dos layouts e sprites produzidos pelo código. A fonte é uma aproximação;
o HUD usa fundo liso para facilitar a inspeção. **Não é uma captura da Game
View** e não valida iluminação, animação ou comportamento do motor.

**Unity não está instalada neste ambiente.** Compilação/importação nativa,
aspecto final, entrada física de mouse/toque, FPS, Android e AAB assinado
continuam pendentes. O processo de exportação permanece descrito em
[PLAY_CONSOLE_RELEASE.md](PLAY_CONSOLE_RELEASE.md).

O HUD agora inclui minimapa e troca de visão durante a partida. Veja [KIN_MOVEMENT_MINIMAP_AND_VIEWS.md](KIN_MOVEMENT_MINIMAP_AND_VIEWS.md).
