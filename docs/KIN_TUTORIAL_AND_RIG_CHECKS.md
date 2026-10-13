# Kin: personagens e tutorial guiado

## Correção do rig

O erro `Bones do not match bindpose` mostrado no Console estava associado à
configuração do `SkinnedMeshRenderer` antes de seus ossos. A atualização atribui
o esqueleto antes da malha, confere a correspondência com as bindposes e mantém
o renderer original até o rig estar configurado. Um cache ausente ou inválido
é reconstruído; malhas incompatíveis continuam com o modelo estático visível.

Essa sequência acompanha o exemplo da [documentação de Mesh.bindposes da Unity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh-bindposes.html).

## Correção dos personagens rosa

Os prints seguintes mostraram a malha rosa e os erros de `pow` e de token `h`
no shader `CatsVsDemons/StylizedModel` em D3D11. O literal `24h` não era um
literal de ponto flutuante válido nesse compilador. Ele foi substituído por
`24.0`; os demais valores com sufixo `h`, inclusive `0h`, foram normalizados
para literais decimais. Os mesmos problemas de `1h`, `3h` e `48h` no shader
`GardenWater` foram corrigidos. Os tipos `half` das variáveis permanecem.

O diagnóstico dos personagens também confere a presença e o suporte dos
shaders dos materiais, além dos erros de compilação conhecidos pela Unity.
A verificação anterior dos ossos, sozinha, não detectava esse problema.
A preparação para Android agora acusa shaders com erros de compilação
conhecidos antes de exportar. Essas verificações não compilam todas as
variantes de todas as plataformas.

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
   confere o renderer, ossos, bindposes, pesos e shaders dos materiais.
5. Confira a presença dos personagens na Game View e a ausência do erro de
   bindposes e de erros de shader no Console. Use uma partida em Isométrico
   para ver Kin. Movimente o personagem e observe as animações de ataque.
6. Complete as seis etapas; teste cliques e toque nos botões e na base marcada.
7. Teste `PULAR` em uma etapa inicial e após construir: as ondas devem iniciar,
   o balão sumir e eventos posteriores não devem reabrir o tutorial.
8. Teste pausa/loja, retomada, notch/safe area e gesto de câmera com dois dedos.
   A câmera não deve acompanhar o alvo distante de nascimento de demônios.

## Verificações realizadas nesta atualização

Sintaxe dos arquivos C# e ordem das atribuições do rig conferidas; arquivo PNG
do retrato íntegro e idêntico à arte original; metadados e diff verificados.

Na correção dos shaders, glslang 12 reproduziu as falhas nos fragmentos originais
dos dois shaders. Após a correção, 18 combinações de estágio/condicional
compilaram e os respectivos módulos SPIR-V passaram pelo `spirv-val`. Esse
teste extrai o HLSL dos passes e usa substitutos mínimos das funções e macros
da URP; ele verifica a sintaxe, sem validar a integração com os includes reais
da Unity, a compilação D3D11 ou a aparência do jogo. A sintaxe dos 66 arquivos
C# foi conferida novamente, sem erros de parser.

O editor Unity continua indisponível no ambiente de desenvolvimento. A
compilação na Unity, a execução do diagnóstico em Play Mode e a validação
visual/Android são pendentes.
