# Roupas e acessórios 3D no gameplay

As referências são as artes existentes `Resources/UI/HouseShopClothes.png` e
`HouseShopAccessories.png`, preservadas. As peças foram modeladas com malhas
próprias em C#, ajustadas ao OBJ de Kin: cabeça grande, torso curto, braços e
pernas curtos. O visual conserva os materiais, silhuetas e símbolos das artes
nessas proporções. As peças usam superfícies, bordados e ornamentos próprios, sem colliders.

| Roupa | Elementos modelados |
| --- | --- |
| Samurai Vermelho | Lapelas claras, placas de armadura vermelha com bordas e rebites dourados, flores, tecido com ondas, obi e franjas. |
| Ninja da Meia-Noite | Roupa escura, detalhes azuis, faixa violeta, cachecol longo, brasões de pata e amarrações. |
| Guardião do Bonsai | Verde e marfim, mangas amplas, folhas nos ombros, robe aberto com bordados de folhas e pingente. |
| Mestre das Lanternas | Vermelho, preto e dourado, chapéu cerimonial, lanternas pequenas, franjas e painéis com chamas. |
| Gato Doméstico | Cardigan claro, botões e bolso com pata, detalhes azuis, lenço vermelho, calças azuis e chinelos de peixe. |
| Ronin Espiritual | Roupa azul escura, ornamentos claros, joia luminosa, manto em camadas com bordas e tecidos de energia azul. |

| Acessório | Elementos modelados ou efeito |
| --- | --- |
| Faixa Samurai | Faixa curva, bordas douradas, placa com pata, nó e pontas de tecido com ondas. |
| Óculos | Aros redondos dourados, ponte, hastes escuras e lentes azuis transparentes. |
| Coleira com Sino | Coleira azul, anéis e sino dourados, fenda do sino, franjas vermelhas e pequeno peixe. |
| Mochila de Peixe | Corpo de carpa, escamas vermelhas e claras, olhos, boca, nadadeiras, cauda, franjas e alças. |
| Amuleto de Proteção | Cordão vermelho, contas, medalhão de jade com pata dourada, aro, tecido e franja. |
| Asas Espirituais | Penas em camadas azuis, hastes claras, articulações douradas, joias e movimento ao caminhar. |
| Passos Mágicos | Pegadas douradas de pata, folhas vermelhas e pontos azuis emitidos pelo deslocamento, com duração limitada. |

## Aplicação no personagem

`KinPrototypeController` aplica a seleção salva quando carrega o modelo. A
loja continua usando o mesmo inventário, preços e sistema de compra/equipar.
`KinShopLoadout` conserva a paleta e solicita as malhas a `KinEquipmentView`.
Trocar de peça remove a geometria anterior; aplicar a mesma seleção conserva
as peças já instaladas. O equipamento não acrescenta colliders nem altera os
atributos e os preços das compras.

As peças ficam sob `GameplayModel/…/Equipped Kin Wardrobe`. A transformação
normalizada usa os limites reais da malha original. A fixação lê ossos e
bindposes do personagem para seguir cabeça, braços, pernas e torso. Tecidos,
manto e asas têm movimento secundário. O fallback de animação original também
mantém o equipamento visível. A primeira pessoa respeita o ocultamento do
corpo; a prévia da loja clona o mesmo modelo equipado, com scripts de gameplay
desativados. A clonagem reutiliza o rig existente sem construir outro por cima.

O rig de Kin agora respeita suas proporções: cabeça na metade superior e
torso/pernas na inferior, sem prender as bochechas aos braços. Os demônios
mantêm seu perfil anterior. A malha original recebe numa cópia um canal de
UV com máscara de cabeça calculada na pose inicial. O shader usa essa máscara
para preservar pelo, olhos e orelhas enquanto a roupa muda de cor. A máscara
é copiada para o rig e independe da altura dos vértices após a animação.

O shader existente recebe cores por vértice, detalhe de fibras, brilho de
metais e emissão para as novas peças. As lentes usam o material transparente
URP Lit, sem escrita de profundidade. Ambos os shaders já entram no fluxo de
exportação. Ossos e bindposes são guardados ao equipar, evitando criar cópias
dessas listas a cada frame. Meshes de equipamento são liberadas ao substituir
as peças e ao destruir o personagem.

Passos mágicos usam um pool de 12 marcas no PC e 8 no celular. Pausa, guia e
loja interrompem o efeito; teleporte não desenha pegadas entre os pontos.
As marcas são projetadas no piso e o equipamento anterior é limpo ao trocar.

## Verificação realizada

- 756 checagens com as classes reais de geometria em perfis PC e celular:
  malhas, índices, cores, UVs, normais, limites e orçamento das peças.
- 1.517 checagens com as classes completas de materiais, equipamento,
  fixação e pegadas em 48 combinações de roupas/acessórios: estado equipado,
  aplicação repetida, rosto preservado, lentes, fallback, primeira pessoa,
  fixação dos óculos no osso animado, pausa, teleporte e limpeza. Incluem
  compra/equipar com o catálogo real e o bloco real de aplicação salvo no
  início da partida, sem alterar os preços.
- 25 checagens dos métodos reais do rig, incluindo o perfil chibi de cabeça,
  mãos e pés; 39 verificações de construção e pausa do guia preservadas.
- 16 combinações de estágio/condicional HLSL compiladas e validadas em SPIR-V
  com substitutos dos includes URP. As checagens C# usam API Unity mínima.
- Prévia de frente e costas renderizada com as malhas reais geradas e o OBJ
  original, fora da Unity, para inspecionar o encaixe. Não representa a
  iluminação, os shaders e a animação da Game View com exatidão.

Por roupa, são 8–11 partes com cerca de 7,3–9,7 mil triângulos no PC e 6,2–8,5
mil no celular. Acessórios acrescentam cerca de 1,2–5,9 mil no PC ou 0,8–5,2
mil no celular. A roupa e o acessório mais detalhados somam aproximadamente
15,5 mil triângulos no PC e 13,7 mil no celular, além do modelo base. As peças
opacas compartilham um material; lentes usam outro. Não é medição de FPS.

**Unity não está instalada neste ambiente.** Compilação com os includes reais,
importação nativa, skinning, ajuste durante ataques, aparência, toque, FPS em
aparelho e AAB assinado ainda precisam ser conferidos no PC.

## Atualizar e conferir na Unity

Feche a Unity, extraia `Cats_vs_Demons_Roupas_Acessorios_3D.zip` e copie a pasta
`Assets` para a raiz do projeto, substituindo os arquivos correspondentes.
Reabra com Unity 6000.5.4f1 e aguarde a importação. O pacote inclui as correções
anteriores de lagos, caminhos, cores do Kin, guia e HUD.

Na loja, compre/equipe uma roupa e um acessório, volte à partida e caminhe,
ataque e use o portal. Confira cada traje e as posições das peças. Reinicie a
partida para conferir a escolha salva. No PC, teste também a câmera em primeira
pessoa e a prévia da loja. Passos Mágicos aparecem quando Kin se desloca.

Em Play Mode, use **Tools → Cats vs Demons → Verificar roupas e acessórios
(Play Mode)** e **Verificar personagens (Play Mode)**. O primeiro confere as
malhas instaladas no personagem real, paleta, lentes e ausência de colliders.
