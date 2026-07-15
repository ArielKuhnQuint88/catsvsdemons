# Game Design Document — Cats vs Demons

> Documento vivo. Valores de balanceamento serão definidos durante os testes.

## 1. Conceito

**Cats vs Demons** é um jogo 3D de ação e Tower Defense. O jogador controla Kin, um gato branco samurai que protege uma casa oriental no centro do mapa. Demônios aparecem nas entradas dos caminhos e avançam continuamente em direção à casa.

Kin combate os inimigos diretamente. Cada demônio derrotado concede moedas, usadas para comprar torres de defesa. As torres possuem poderes ativados ou potencializados quando Kin está próximo, fazendo o jogador alternar entre combate, posicionamento e apoio às defesas.

![Direção de arte 3D atual](images/current-3d-art-direction.png)

## 2. Objetivo

Sobreviver às ondas de demônios e impedir que eles alcancem e destruam a casa oriental.

## 3. Loop principal

1. Iniciar uma onda.
2. Gerar demônios nas entradas do mapa.
3. Movê-los pelos caminhos até a casa.
4. Controlar Kin e atacar os inimigos.
5. Receber moedas pelos demônios derrotados.
6. Comprar e posicionar torres.
7. Aproximar Kin das torres para ativar seus poderes.
8. Usar portais, bonsais e lanternas estrategicamente.
9. Eliminar a onda e preparar a próxima.

## 4. Mapa

- Casa oriental posicionada no centro.
- Um ou mais caminhos ligam as entradas do mapa à casa.
- Demônios surgem no início desses caminhos.
- Áreas de construção permitem comprar e posicionar torres.
- Portais, bonsais e lanternas ocupam pontos estratégicos.
- Todos os caminhos dos inimigos convergem para a casa.

## 5. Kin

- Gato branco samurai controlado diretamente pelo jogador.
- Roupa verde-escura com detalhes vermelhos e acessórios dourados.
- Pode se movimentar, atacar demônios e usar elementos do mapa.
- Possui pontos de vida.
- Sua proximidade ativa ou melhora os poderes das torres.
- Recupera vida ao usar ou permanecer próximo de bonsais.
- Viaja rapidamente por meio dos portais.

## 6. Demônios

- **Sonegron:** demônio roxo associado ao sono.
- **Poerix:** demônio felpudo associado à poeira.
- **Flamurk:** demônio associado ao fogo.

Comportamento básico:

1. Surge em uma entrada.
2. Seleciona ou recebe um caminho.
3. Avança sempre em direção à casa.
4. Pode ser atacado por Kin e pelas torres.
5. Ao ser derrotado, concede moedas.
6. Ao alcançar a casa, causa dano.

Atributos previstos:

- Vida
- Velocidade
- Dano contra a casa
- Recompensa em moedas
- Resistências ou habilidades específicas

## 7. Economia e torres

- Demônios derrotados concedem moedas.
- As moedas são usadas durante a partida para comprar torres.
- Cada torre possui custo, alcance e poder próprios.
- O poder especial da torre depende da proximidade de Kin.
- O jogador precisa decidir entre lutar em outro caminho ou permanecer perto de uma torre para fortalecê-la.

Tipos, valores e melhorias das torres ainda serão definidos.

## 8. Elementos especiais

### Portais

Permitem transportar Kin:

- para áreas ao redor do portal utilizado;
- para áreas ao redor de outros portais conectados.

### Bonsais

Recarregam a vida de Kin.

### Lanternas

Deixam os demônios próximos mais lentos.

## 9. Progressão da partida

- As ondas aumentam gradualmente a quantidade e a dificuldade dos inimigos.
- Intervalos entre ondas permitem comprar defesas e se reposicionar.
- A casa possui vida própria.
- A vitória ocorre ao concluir todas as ondas.
- A derrota ocorre quando a vida da casa chega a zero.

## 10. Pilares

1. Combate direto com Kin.
2. Defesa e posicionamento estratégico de torres.
3. Movimento rápido pelo mapa usando portais.
4. Sinergia entre Kin e as estruturas defensivas.
5. Leitura visual clara dos caminhos, ameaças e áreas de efeito.

## 11. Histórico

O projeto começou em **2016**, durante o curso Técnico em Programação de Jogos Digitais do SENAI. A primeira versão foi programada em **Java** e usava arte 2D.

![Painel da versão Java de 2016](images/java-2016-game-concept.png)

A versão atual migra para Unity 6.5 e adota personagens, cenários e peças em 3D.

## 12. Direção visual

Fantasia 3D estilizada, com proporções fofas e leitura clara. O cenário combina uma casa oriental, caminhos bem definidos, vegetação, lanternas, bonsais e portais. Cada demônio possui cor e silhueta próprias para identificação rápida.

## 13. Perguntas em aberto

- Quantidade de caminhos e entradas
- Forma de ataque e habilidades de Kin
- Tipos e poderes das torres
- Distância necessária para Kin ativar cada torre
- Custo das torres e recompensa dos inimigos
- Número e duração das ondas
- Vida da casa e de Kin
- Regras exatas de uso e conexão dos portais
- Habilidades específicas de Sonegron, Poerix e Flamurk
