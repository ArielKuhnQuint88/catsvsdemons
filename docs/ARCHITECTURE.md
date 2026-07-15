# Arquitetura técnica

## Objetivo

Construir um protótipo local de Tower Defense em Unity no qual Kin combate demônios, protege a casa central, compra torres e interage com portais, bonsais e lanternas.

## Princípios

- Sistemas de gameplay separados da interface.
- Dados de inimigos, torres e ondas configuráveis por ScriptableObjects.
- Comunicação entre sistemas por eventos.
- Pooling de inimigos e projéteis.
- Caminhos dos inimigos independentes da lógica visual.
- Valores de balanceamento editáveis sem alterar código.
- Primeiro protótipo local; multiplayer fica fora do MVP.

## Fluxo principal

```text
WaveManager
    ↓
EnemySpawner
    ↓
Enemy follows Path
    ↓
Kin + Towers attack
    ↓
Enemy defeated → Coins
    ↓
Build / Upgrade Towers
    ↓
Protect House
```

## Módulos sugeridos

### Core

- `GameManager`
- `GameState`
- `EventBus`
- `ObjectPool`

### Player

- `KinController`
- `KinMovement`
- `KinCombat`
- `KinHealth`
- `KinProximitySensor`

### Enemies

- `EnemyController`
- `EnemyMovement`
- `EnemyHealth`
- `EnemyAttack`
- `EnemyData`

### Waves

- `WaveManager`
- `EnemySpawner`
- `WaveData`
- `SpawnEntry`
- `PathController`

### House

- `HouseHealth`
- `HouseDamageReceiver`
- `DefeatController`

### Economy

- `Wallet`
- `CoinReward`
- `TowerShop`
- `BuildSpot`

### Towers

- `TowerController`
- `TowerTargeting`
- `TowerAttack`
- `TowerData`
- `TowerProximityPower`

### Map Elements

- `Portal`
- `PortalNetwork`
- `BonsaiHealingZone`
- `LanternSlowZone`

### UI

- Vida de Kin
- Vida da casa
- Moedas
- Onda atual
- Loja de torres
- Indicadores de áreas de efeito

## Dados configuráveis

### EnemyData

- Vida
- Velocidade
- Dano
- Recompensa
- Prefab
- Habilidades

### TowerData

- Custo
- Alcance
- Dano ou efeito
- Cadência
- Poder de proximidade
- Prefab

### WaveData

- Tipos de inimigos
- Quantidades
- Intervalos
- Entradas e caminhos
- Recompensa da onda

## Ordem de implementação

1. Casa central com vida.
2. Um caminho e uma entrada.
3. Um demônio seguindo o caminho.
4. Kin com movimento, ataque e vida.
5. Dano e recompensa em moedas.
6. Uma torre comprável.
7. Poder da torre ativado pela proximidade de Kin.
8. Sistema de ondas.
9. Portais.
10. Bonsais.
11. Lanternas.
12. Novos inimigos, torres e caminhos.

## Estrutura Unity

```text
Assets/_Project/
├── Art/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── ScriptableObjects/
│   ├── Enemies/
│   ├── Towers/
│   └── Waves/
├── Scripts/
│   ├── Core/
│   ├── Combat/
│   ├── Economy/
│   ├── Enemies/
│   ├── House/
│   ├── Map/
│   ├── Player/
│   ├── Towers/
│   ├── UI/
│   └── Waves/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

## Segurança

Nunca versionar chaves, tokens, senhas, keystores ou arquivos locais de serviços.
