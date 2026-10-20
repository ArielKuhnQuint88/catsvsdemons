# Lagos separados dos caminhos e cores do Kin

Os lagos usavam duas posições fixas. As margens de pedra e as pontes podiam
cobrir caminhos que mudam com a fase e a câmera. Agora o cenário escolhe locais
livres após receber as rotas definitivas. Água, pedras, ponte e corrimões ficam
numa área reservada com raio de 3,4 metros, acrescida da largura máxima das
curvas do caminho e de 0,75 metro de folga. As bases de torres e construções
também são preservadas. Se uma disposição personalizada não tiver espaço,
o lago é omitido, sem cobrir o caminho.

O terreno fica plano sob os lagos, inclusive nas células junto à margem.
Árvores e plantas usam as mesmas posições escolhidas para evitar invasões.
As rotas, colliders e trajetos dos inimigos continuam sincronizados.

O equipamento aplicava o destaque dourado às quatro superfícies de Kin.
Agora as superfícies importadas usam suas próprias cores, com as faixas de
altura explicitamente desativadas: pelagem clara, roupa vermelha, tecido azul
esverdeado e partes escuras no traje padrão. Trocar de roupa altera os tecidos
e mantém a pelagem. A textura de fibras continua aplicada. O fallback para
importação com uma única superfície mantém a cabeça clara e a roupa vermelha.

## Verificação

- 72 combinações de cenário, estação, perfil e extensão de entrada, usando
  rotas e bases extraídas da cena: 8.957 assertivas de geometria. Conferem a
  área completa dos lagos contra cada triângulo de borda dos caminhos, espaço
  para as bases, terreno sob a água e orçamento de malhas. Uma disposição
  artificial totalmente ocupada não cria lagos sobre os caminhos.
- 227 verificações com as classes completas de materiais e equipamento,
  incluindo os renderers original e animado, seis roupas, sete acessórios e
  textura preservada. O teste de equipamento falha na versão anterior quando
  encontra o destaque dourado sobre a superfície da pelagem e passa na correção.
- 39 verificações anteriores de construção e pausa do guia preservadas.
- 16 combinações de estágio/condicional HLSL compiladas com glslang e
  validadas em SPIR-V usando substitutos dos includes URP.

As checagens C# usam substitutos mínimos da API Unity. **Não há Unity instalada
neste ambiente:** importação nativa, compilação com os includes reais, aparência
na Game View, interação em aparelho e AAB assinado precisam de validação no PC.

## Atualizar no PC

Feche a Unity, extraia `Cats_vs_Demons_Lagos_Kin_Corrigidos.zip` e copie a pasta
`Assets` para a raiz do projeto, substituindo os arquivos correspondentes.
Reabra com Unity 6000.5.4f1 e aguarde a importação. O pacote inclui o guia, HUD,
personagens e cenário das revisões anteriores.

Inicie uma partida e confira os lagos e Kin. Em Play Mode, os menus
**Tools → Cats vs Demons → Verificar cenário e caminhos (Play Mode)** e
**Verificar personagens (Play Mode)** conferem também estas correções.
Teste a troca de roupas na loja e a progressão das fases. O tutorial permanece
disponível pelo botão **GUIA ?** ou por **F1** no PC.
