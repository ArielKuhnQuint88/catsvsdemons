# Game Design Document — Cats vs Demons

> Documento vivo. As regras serão validadas no protótipo digital.

## 1. Conceito

**Cats vs Demons** é um jogo de estratégia por turnos em um tabuleiro digital. Os jogadores controlam Kin, um gato branco samurai, enquanto diferentes demônios avançam e alteram o estado da partida.

![Direção de arte 3D atual](images/current-3d-art-direction.png)

## 2. Histórico

O projeto começou em **2016**, durante o curso Técnico em Programação de Jogos Digitais do SENAI. A primeira versão foi programada em **Java** e usava arte 2D. O processo já envolvia GDD, Scrum, concept art, protótipos digitais e uma adaptação física.

![Painel da versão Java de 2016](images/java-2016-game-concept.png)

A versão atual migra para Unity 6.5 e adota personagens e peças em 3D.

## 3. Pilares

1. Regras fáceis de compreender.
2. Decisões táticas a cada turno.
3. Partidas rejogáveis com eventos e posicionamentos variáveis.
4. Identidade visual forte, misturando gatos, fantasia e demônios.
5. Mesma lógica de jogo em PC, celular e realidade virtual.

## 4. Personagens e peças

### Kin

- Gato branco.
- Samurai com roupa verde-escura, detalhes vermelhos e acessórios dourados.
- Retorna ao centro quando atingido por um demônio.

### Demônios

- **Sonegron:** demônio roxo associado ao sono; entra em um caminho laranja.
- **Poerix:** demônio felpudo associado à poeira; entra em um caminho laranja.
- **Flamurk:** demônio de fogo; entra em um caminho laranja.

Os comportamentos e habilidades individuais serão definidos durante a prototipação.

### Elementos especiais

- **Portal:** entra em um quadrado azul e transporta personagens entre pontos conectados.
- **Bonsai:** entra em um quadrado azul; função estratégica a detalhar.
- **Lanterna:** entra em um quadrado azul; função estratégica a detalhar.
- **Torre:** atua nas oito casas adjacentes.

## 5. Fluxo provisório do turno

1. Revelar ou resolver uma carta.
2. Posicionar a peça ou aplicar o evento indicado.
3. Resolver a movimentação dos demônios.
4. Resolver a movimentação de Kin.
5. Aplicar colisões, remoções e efeitos das casas.
6. Verificar vitória ou derrota.
7. Passar o turno.

## 6. Regras conhecidas do protótipo físico

- Kin começa no centro.
- O dado vermelho movimenta os demônios.
- O dado azul movimenta Kin.
- Quando um demônio alcança um jogador, ele retorna ao centro.
- Quando o jogador termina antes ou depois do demônio, o demônio é removido.
- Torres afetam as oito casas adjacentes.
- Portais permitem transporte.

Essas regras devem ser tratadas como hipóteses até serem testadas no protótipo digital.

## 7. Modos planejados

- Protótipo local
- Multiplayer
- PC
- Android
- Realidade virtual em etapa posterior

## 8. Direção visual

Fantasia 3D estilizada, com proporções fofas e leitura clara das peças. Kin é reconhecível pela pelagem branca, olhos grandes, roupa verde-escura, faixa vermelha e acessórios dourados. Cada demônio possui cor e silhueta próprias para ser identificado rapidamente no tabuleiro.

## 9. Perguntas em aberto

- Condição exata de vitória e derrota
- Quantidade de jogadores
- Tamanho e formato do tabuleiro
- Função definitiva de bonsais e lanternas
- Habilidades específicas de Sonegron, Poerix e Flamurk
- Duração desejada de uma partida
- Progressão e recompensas
