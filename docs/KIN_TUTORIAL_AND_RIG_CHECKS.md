# Kin: personagens e tutorial guiado

## Correção do rig

O erro `Bones do not match bindpose` mostrado no Console estava associado à
configuração do `SkinnedMeshRenderer` antes de seus ossos. A atualização atribui
o esqueleto antes da malha, confere a correspondência com as bindposes e mantém
o renderer original até o rig estar configurado. Um cache ausente ou inválido
é reconstruído; malhas incompatíveis continuam com o modelo estático visível.

Essa sequência acompanha o exemplo da [documentação de Mesh.bindposes da Unity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh-bindposes.html).

## Tutorial

- Retrato de Kin reutilizado da arte original do ícone, com transparência e
  pequeno movimento durante a fala. As falas aparecem gradualmente no balão.
- Objetivo e progresso separados da fala, com marcador no mundo ou moldura
  pulsante no botão correspondente. Alvos fora da área de jogo recebem seta.
- A câmera se aproxima suavemente de Kin, da base ou da Lanterna durante as
  etapas. No combate do tutorial, acompanha Kin. Um gesto de dois dedos cede
  o controle ao jogador, e finalizar/pular restaura a visão normal.
- Construção recebe entrada direta de mouse/toque pelo Input System; os
  destaques não bloqueiam cliques e a área do joystick mantém sua função.
- A conclusão usa `TutorialCompletedV2`, para apresentar o novo tutorial mesmo
  quando o tutorial anterior foi concluído. `REPETIR TUTORIAL` nas configurações
  do menu reativa o guia na próxima partida. `PULAR` libera as ondas e remove
  os marcadores sem esperar a conclusão de uma etapa.
- O guia pausa quando a casa, a loja ou a tela de pausa estão abertas. Se
  faltarem moedas para a Lanterna inicial, somente a diferença é concedida.

## Verificar na Unity

1. Atualize a branch `feature/android-release-polish` com a Unity fechada.
2. Abra a raiz do projeto em Unity 6000.5.4f1 e aguarde a importação.
3. No menu do jogo, abra Configurações e pressione `REPETIR TUTORIAL`. Inicie uma partida.
4. Em Play Mode, execute **Tools > Cats vs Demons > Verificar personagens (Play Mode)**.
   O diagnóstico instancia Kin e os três tipos de demônio fora do mapa e
   confere o renderer, ossos, bindposes e os pesos de todos os vértices.
5. Confira a presença dos personagens na Game View e a ausência do erro de
   bindposes no Console. Movimente Kin e observe as animações de ataque.
6. Complete as seis etapas; teste cliques e toque nos botões e na base marcada.
7. Teste `PULAR` em uma etapa inicial e após construir: as ondas devem iniciar,
   o balão sumir e eventos posteriores não devem reabrir o tutorial.
8. Teste pausa/loja, retomada, notch/safe area e gesto de câmera com dois dedos.
   A câmera não deve acompanhar o alvo distante de nascimento de demônios.

## Verificações realizadas nesta atualização

Sintaxe dos arquivos C# e ordem das atribuições do rig conferidas; arquivo PNG
do retrato íntegro e idêntico à arte original; metadados e diff verificados.
O editor Unity continua indisponível no ambiente de desenvolvimento. A execução
do diagnóstico em Play Mode e a validação visual/Android são pendentes.
