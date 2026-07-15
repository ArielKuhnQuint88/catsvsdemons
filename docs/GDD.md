# Game Design Document — Cats vs Demons

> Documento vivo. As regras serão validadas no protótipo digital.

## 1. Conceito

**Cats vs Demons** é um jogo de estratégia por turnos em um tabuleiro digital. Os jogadores controlam Kin, um gato branco que à noite se transforma em samurai, enquanto diferentes demônios avançam e alteram o estado da partida.

## 2. Pilares

1. Regras fáceis de compreender.
2. Decisões táticas a cada turno.
3. Partidas rejogáveis com eventos e posicionamentos variáveis.
4. Identidade visual forte, misturando gatos, samurais e demônios.
5. Mesma lógica de jogo em PC, celular e realidade virtual.

## 3. Personagens e peças

### Kin

- Gato branco.
- Forma samurai com roupas vermelhas.
- Espada dourada.
- Retorna ao centro quando atingido por um demônio.

### Demônios

- Demônio do Sono
- Demônio da Poeira
- Demônio do Fogo

Cada tipo deverá receber comportamento, habilidade e identidade visual próprios durante a prototipação.

### Elementos especiais

- **Portal:** transporta personagens entre pontos conectados.
- **Bonsai:** elemento estratégico a detalhar.
- **Lanterna:** elemento estratégico a detalhar.
- **Torre:** atua nas oito casas adjacentes.

## 4. Fluxo provisório do turno

1. Revelar ou resolver uma carta.
2. Posicionar a peça ou aplicar o evento indicado.
3. Resolver a movimentação dos demônios.
4. Resolver a movimentação de Kin.
5. Aplicar colisões, remoções e efeitos das casas.
6. Verificar vitória ou derrota.
7. Passar o turno.

## 5. Regras conhecidas do protótipo físico

- Kin começa no centro.
- O dado vermelho movimenta os demônios.
- O dado azul movimenta Kin.
- Quando um demônio alcança um jogador, ele retorna ao centro.
- Quando o jogador termina antes ou depois do demônio, o demônio é removido.
- Torres afetam as oito casas adjacentes.
- Portais permitem transporte.

Essas regras devem ser tratadas como hipóteses até serem testadas no protótipo digital.

## 6. Modos planejados

- Protótipo local
- Multiplayer
- PC
- Android
- Realidade virtual em etapa posterior

## 7. Direção visual

Fantasia estilizada com contraste entre o cotidiano felino e o mundo sobrenatural noturno. Kin deve ser reconhecível pela silhueta branca, roupa vermelha e espada dourada.

## 8. Perguntas em aberto

- Condição exata de vitória e derrota
- Quantidade de jogadores
- Tamanho e formato do tabuleiro
- Função definitiva de bonsais e lanternas
- Diferenças mecânicas entre os tipos de demônio
- Duração desejada de uma partida
- Progressão e recompensas
