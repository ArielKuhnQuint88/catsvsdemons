# Arquitetura técnica

## Objetivo

Construir primeiro um protótipo local confiável e manter a lógica preparada para multiplayer sem acoplar as regras à interface ou ao serviço de rede.

## Princípios

- Servidor autoritativo no multiplayer.
- Estado único e serializável da partida.
- Jogadores enviam ações; a autoridade valida e produz eventos.
- Aleatoriedade controlada por seed.
- Snapshots ocasionais para recuperação e sincronização.
- Regras independentes de GameObjects e cenas Unity.
- Interface reage ao estado; não decide as regras.

## Fluxo

```text
PlayerAction
    ↓
ActionValidator
    ↓
GameRules
    ↓
GameEvent[]
    ↓
GameState
    ↓
View / UI
```

## Módulos sugeridos

### Core

- `GameState`
- `MatchSettings`
- `PlayerState`
- `BoardState`
- `DeterministicRandom`

### Gameplay

- `TurnSystem`
- `MovementSystem`
- `CardSystem`
- `DiceSystem`
- `CombatSystem`
- `VictorySystem`

### Actions

- `RollDiceAction`
- `MoveKinAction`
- `PlacePieceAction`
- `UsePortalAction`
- `EndTurnAction`

### Events

- `DiceRolledEvent`
- `PieceMovedEvent`
- `DemonRemovedEvent`
- `PlayerReturnedToCenterEvent`
- `TurnEndedEvent`
- `MatchFinishedEvent`

### Presentation

- Tabuleiro e peças
- Animação
- Áudio
- UI e feedback
- Input específico de cada plataforma

### Networking

A camada de rede deverá transportar ações, eventos e snapshots. A primeira versão será local. Photon Fusion e Unity Netcode + Relay serão comparados somente após a lógica central estar testável.

## Estrutura Unity

```text
Assets/_Project/
├── Art/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── ScriptableObjects/
├── Scripts/
│   ├── Core/
│   ├── Gameplay/
│   ├── Networking/
│   ├── Presentation/
│   └── UI/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

## Segurança

Nunca versionar chaves, tokens, senhas, keystores ou arquivos locais de serviços. Usar variáveis de ambiente e arquivos de exemplo sem valores reais.
