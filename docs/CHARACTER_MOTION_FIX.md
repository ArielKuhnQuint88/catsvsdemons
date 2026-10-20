# Movimento e animação dos personagens — 1.0.2 / código 3

## Comportamento corrigido

Na visão isométrica, WASD, setas e joystick fazem Kin andar e virar para a
direção escolhida, inclusive para baixo e nas diagonais. O ataque automático
só pode orientar o corpo quando o jogador solta o movimento. Os eixos
continuam relativos à câmera, com velocidade uniforme nas oito direções.
Na primeira pessoa, S continua recuando e A/D continuam deslizando conforme
o olhar, sem girar a câmera em direção aos inimigos.

O problema vinha de duas regras: `TurnWithMovement` bloqueava o giro para
baixo e durante combate; em seguida, `KinPrototypeAttack.Update` girava o
personagem para o alvo, sobrescrevendo a direção do controlador.

## Animação

- Kin conserva o rig e os equipamentos ligados aos ossos. Os braços e pernas
  trabalham em oposição, os joelhos dobram gradualmente e o tronco usa os
  eixos corretos do modelo importado em Z-up.
- O golpe dura 0,38 s, com preparação, impacto e recuperação suaves. Corpo,
  braço, pata e katana compartilham a mesma curva. Caminhar continua durante
  o golpe; ao terminar, os braços retornam à pose atual da caminhada.
- A pata em primeira pessoa prepara o golpe antes de avançar. Sua oscilação
  acompanha a velocidade real, em vez de usar um relógio independente.
- Sono, Poerix e Flamurk usam os modelos estáticos originais e animações
  corporais próprias: flutuação leve, marcha pesada e passos mais vivos.
  Rotação e pequenos deslocamentos dão movimento sem esticar a geometria.
  Esses modelos não têm rig de membros autorado; a correção remove o auto-rig
  humano genérico que cortava os rostos redondos em regiões de braços/pernas.
- A escala permanece igual à importada em todos os quadros. O corpo gira
  ao redor do ponto no chão e a animação de ataque se soma à caminhada.
- Os demônios viram para o alvo quando atacam parados. Pausa congela os
  ataques e poses; teleporte não é interpretado como uma passada enorme.

## Atualizar no PC

1. Feche a Unity e preserve alterações próprias nos arquivos correspondentes.
2. Extraia o pacote e copie seu conteúdo para a raiz do projeto existente,
   junto de `Assets`, `Packages` e `ProjectSettings`. Aceite substituir.
3. Abra o projeto com Unity **6000.5.4f1**, aguarde a importação e abra
   `Assets/_Project/Scenes/MainMenu.unity`.
4. Entre na partida e teste WASD/setas e diagonais, inclusive ao lado de um
   demônio. Kin deve orientar o corpo conforme o comando.
5. Solte os controles: o ataque automático pode orientar Kin para seu alvo.
   Ande novamente: a direção do jogador volta a ter prioridade.
6. Teste caminhar e golpear, alternar com V, recuar na primeira pessoa,
   joystick e olhar no Android, pausa e retorno do guia/tutorial.
7. Confira os três demônios em movimento e atacando. Rostos, chifres e corpos
   devem manter suas proporções. Use também `Tools > Cats vs Demons >
   Verificar personagens (Play Mode)` e o verificador de movimento/câmera.

## APK e Play Console

`EXPORTAR_APK.cmd` continua gerando um aplicativo de teste independente.
Para a Play Console, gere **um novo AAB** com `EXPORTAR_PLAY_CONSOLE.cmd`.
O AAB anterior de código 2 não contém estas correções. A versão agora tem
código base **3**; o exportador também respeita códigos superiores que já
tenham sido enviados. O pacote de produção permanece `com.qipgames.catsvsdemons`.

Use a mesma chave de upload cadastrada no pedido de redefinição e aguarde sua
ativação conforme o aviso do Play Console. Alterações de movimento não exigem
outra chave ou outra redefinição.

## Validação realizada e limites

- **1.174** verificações dos helpers reais de direção, aceleração, curvas de
  ataque e perfis corporais, com tipos matemáticos mínimos substituindo a API
  Unity. Podem ser executadas com .NET 8:
  `dotnet run --project Tools/Validation/CharacterMotionChecks/CharacterMotionChecks.csproj`.
- **2.295** verificações de integração das classes reais de controle, ataque,
  animação, câmera, minimapa e alertas, usando fixtures mínimas da API Unity.
  Incluem rotação nas oito direções com alvo vivo, joystick, recuperação do
  golpe, escala preservada, pausa e teleporte.
- **337** verificações da interface e ações dos menus em cinco layouts.
- A compilação/importação na Unity, o visual da Game View, o desempenho e o
  teste físico no Android permanecem pendentes. Os testes de fixtures não
  substituem o motor. O pacote contém fontes e exportadores, sem APK/AAB novo
  compilado neste ambiente.

A semântica de rotação local usada na correção segue a documentação da Unity:
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Transform-localRotation.html
