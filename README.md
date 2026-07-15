# Cats vs Demons

Jogo digital de tabuleiro e estratégia desenvolvido pela **QiP Games**.

Durante a noite, **Kin**, um gato branco, assume sua forma de samurai. Sua missão é enfrentar demônios, proteger o tabuleiro e impedir que o caos tome conta do mundo.

![Direção de arte 3D atual de Cats vs Demons](docs/images/current-3d-art-direction.png)

## Status

🧪 Pré-produção / protótipo digital

O projeto está começando sua implementação em **Unity**, inspirado no protótipo físico desenvolvido e testado com peças impressas em 3D.

## Evolução do projeto

Cats vs Demons nasceu em **2016** como um projeto do curso Técnico em Programação de Jogos Digitais do **SENAI**, com uma primeira versão 2D programada em **Java**. O desenvolvimento já incluía GDD, Scrum, concept art, protótipos digitais e uma adaptação física de tabuleiro.

![Painel do projeto original em Java, 2016](docs/images/java-2016-game-concept.png)

A versão atual retoma esse conceito com nova direção de arte 3D, regras revisadas e implementação em Unity 6.5. O objetivo do repositório é documentar publicamente essa evolução e apresentar o processo como portfólio de game design e programação.

## Visão do jogo

- Estratégia por turnos
- Tabuleiro digital
- Partidas para múltiplos jogadores
- Regras simples e decisões táticas
- Plataformas planejadas: PC, celular e realidade virtual
- Estado de partida determinístico e preparado para multiplayer autoritativo

## Elementos principais

### Herói

- **Kin** — gato branco samurai.

### Demônios

- **Sonegron** — associado ao sono.
- **Poerix** — associado à poeira.
- **Flamurk** — associado ao fogo.

### Elementos do tabuleiro

- Portais
- Bonsais
- Lanternas
- Torres e áreas de proteção

## Tecnologia planejada

- Unity **6.5 (6000.5.4f1)**
- Universal Render Pipeline (URP)
- C#
- Multiplayer baseado em ações e eventos
- Estado único e serializável
- Seed determinística
- Snapshots ocasionais para sincronização

A solução de rede será definida durante o protótipo, considerando Photon Fusion e Unity Netcode + Relay.

## Estrutura planejada

```text
Assets/
└── _Project/
    ├── Art/
    ├── Audio/
    ├── Materials/
    ├── Prefabs/
    ├── Scenes/
    ├── Scripts/
    │   ├── Core/
    │   ├── Gameplay/
    │   ├── Networking/
    │   └── UI/
    └── Tests/
Packages/
ProjectSettings/
docs/
```

## Roadmap inicial

- [x] Definir versão da Unity
- [ ] Criar projeto-base
- [ ] Modelar o estado serializável da partida
- [ ] Implementar tabuleiro e movimentação
- [ ] Implementar Kin e demônios
- [ ] Implementar cartas, dados e turnos
- [ ] Criar protótipo local jogável
- [ ] Adicionar testes das regras
- [ ] Prototipar multiplayer
- [ ] Preparar builds para PC e Android

## Documentação

- [Game Design Document](docs/GDD.md)
- [Arquitetura técnica](docs/ARCHITECTURE.md)
- [Como contribuir](CONTRIBUTING.md)
- [Game Concept original no Prezi](https://prezi.com/view/dgeKHwZEjqjX1hIC55NR/?referral_token=8JTueZlnB3FN)

## Autoria

Projeto criado por **Ariel Kühn Quint** e desenvolvido pela **QiP Games**.

Todos os direitos reservados. Nenhuma licença de uso foi concedida neste repositório.
