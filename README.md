# Cats vs Demons

Jogo digital de tabuleiro e estratégia desenvolvido pela **QiP Games**.

Durante a noite, **Kin**, um gato branco, assume sua forma de samurai: roupas vermelhas e uma espada dourada. Sua missão é enfrentar demônios, proteger o tabuleiro e impedir que o caos tome conta do mundo.

## Status

🧪 Pré-produção / protótipo digital

O projeto está começando sua implementação em **Unity**, inspirado no protótipo físico desenvolvido e testado com peças impressas em 3D.

## Visão do jogo

- Estratégia por turnos
- Tabuleiro digital
- Partidas para múltiplos jogadores
- Regras simples e decisões táticas
- Plataformas planejadas: PC, celular e realidade virtual
- Estado de partida determinístico e preparado para multiplayer autoritativo

## Elementos principais

### Herói

- **Kin** — gato branco que se transforma em samurai durante a noite.

### Demônios

- Demônio do Sono
- Demônio da Poeira
- Demônio do Fogo

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

## Autoria

Projeto criado por **Ariel Kühn Quint** e desenvolvido pela **QiP Games**.

Todos os direitos reservados. Nenhuma licença de uso foi concedida neste repositório.
