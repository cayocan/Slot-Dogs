# Slot Engine — Documentação Técnica

Projeto monorepo de slot machine composto por três partes independentes:

| Módulo | Tecnologia | Responsabilidade |
|---|---|---|
| `slot-dogs-backend` | Node.js + Fastify | Lógica de jogo, RNG provably fair, API REST |
| `slot-dogs-unity` | Unity + C# | Engine de slot machine genérica + jogo Slot Dogs |
| `slot-dogs-frontend` | Next.js + React | Shell web que embute o build WebGL |

---

## Índice

1. [Arquitetura geral](#1-arquitetura-geral)
2. [Backend](#2-backend)
3. [Unity — Engine](#3-unity--engine)
4. [Unity — Jogo Slot Dogs](#4-unity--jogo-slot-dogs)
5. [Frontend](#5-frontend)
6. [Como implementar um novo jogo](#6-como-implementar-um-novo-jogo)

---

## 1. Arquitetura geral

```
Frontend (Next.js)
    │  embed iframe WebGL
    ▼
Unity WebGL Build
    │  MVP + State Machine
    ▼
Backend (Fastify REST API)
    │  POST /spin → resultado do giro
    ▼
Engine de cálculo (Node.js)
    RNG provably fair (HMAC-SHA256)
```

O cliente Unity **nunca calcula resultados** — envia intenções (spin, betPerLine) e exibe o que o backend retorna. Toda lógica de jogo reside no servidor.

---

## 2. Backend

### Stack

- **Runtime:** Node.js 20+
- **Framework:** Fastify (schema validation nativa)
- **Logger:** Pino (JSON estruturado)
- **RNG:** `crypto.randomBytes` nativo (CSPRNG)
- **Provably Fair:** HMAC-SHA256

### Endpoints principais

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/session` | Cria sessão (100 moedas iniciais) |
| `GET` | `/session/:id` | Consulta estado da sessão |
| `POST` | `/session/:id/seed` | Define client seed (mín. 8 chars) |
| `POST` | `/spin` | Executa um giro |
| `POST` | `/session/:id/rotate` | Rotaciona server seed (revela seed anterior) |

### Estrutura de pastas

```
slot-dogs-backend/
├── index.js              # Entry point
└── src/
    ├── app.js            # Fastify instance + plugins
    ├── server.js         # Bind porta
    ├── engine/
    │   ├── config.js     # Símbolos, paylines, pesos dos reels, paytable
    │   └── spinEngine.js # Cálculo do giro (grid, lineWins, scatter, free spins)
    ├── routes/           # Handlers HTTP
    ├── provablyfair/     # HMAC-SHA256, geração/verificação de seeds
    └── rng/              # Wrapper de crypto.randomBytes
```

### Configuração do jogo (`src/engine/config.js`)

Para um novo jogo, este é o único arquivo que muda no backend:

```js
const SYMBOL_IDS = { ... };      // mapeamento nome → id
const PAYTABLE   = { ... };      // multiplicadores por símbolo (3, 4, 5 iguais)
const REEL_WEIGHTS = [ ... ];    // peso de cada símbolo em cada reel (5 arrays)
const PAYLINES   = [ ... ];      // linhas de pagamento (path = índice de linha por coluna)
```

### Iniciar o servidor

```powershell
cd slot-dogs-backend
npm install
node index.js          # porta 3000 por padrão
```

---

## 3. Unity — Engine

### Localização

```
slot-dogs-unity/Assets/Scripts/Engine/
├── Core/
│   ├── ISlotMachineView.cs   # Contrato da View (o que os estados enxergam)
│   ├── ISpinProvider.cs      # Contrato de execução de spin (abstrai API)
│   ├── ISpinResult.cs        # Marcador genérico para resultado de spin
│   ├── IGameModel.cs         # Contrato do model (Coins, FreeSpins, LastSpin)
│   ├── ISlotState.cs         # Interface base de estado
│   ├── SlotGameContext.cs    # Contexto compartilhado entre estados
│   └── SlotStateMachine.cs   # Máquina de estados (Enter/Exit/Transition)
├── States/
│   ├── SlotStateBase.cs      # Classe base com helpers comuns
│   ├── IdleState.cs          # Aguarda input; dispara auto-spin se ativo
│   ├── SpinningState.cs      # Chama API + animação de giro em paralelo
│   ├── ShowResultState.cs    # Decide próximo estado (Idle ou FreeSpins)
│   └── FreeSpinsState.cs     # Loop automático de rodadas gratuitas
├── Data/
│   └── SlotMachineConfig.cs  # ScriptableObject de configuração por jogo
└── Audio/
    └── AudioManager.cs       # Singleton genérico de áudio com pool de sources
```

### Fluxo da máquina de estados

```
           ┌─────────────────────────────────────────┐
           │                IdleState                │
           │  Aguarda click em Spin ou auto-spin      │
           └──────────────────┬──────────────────────┘
                              │ OnSpinRequested
                              ▼
           ┌─────────────────────────────────────────┐
           │              SpinningState              │
           │  API + animação em paralelo (WhenAll)   │
           │  Ctx.SpinProvider.SpinAsync()           │
           └──────────────────┬──────────────────────┘
                              │ resultado disponível
                              ▼
           ┌─────────────────────────────────────────┐
           │            ShowResultState              │
           │  Atualiza moedas, verifica free spins   │
           └────────┬─────────────────────┬──────────┘
         sem FS     │                     │ FreeSpinsRemaining > 0
                    ▼                     ▼
              IdleState           FreeSpinsState
                                  (loop automático)
                                        │ FS = 0
                                        ▼
                                   IdleState
```

### Interfaces da engine (contratos)

**`ISlotMachineView`** — o que os estados podem pedir à View:
```csharp
void SetSpinInteractable(bool interactable);
void UpdateCoins(int coins);
void UpdateFreeSpins(int remaining);
void SetAutoSpinActive(bool active);
void StartSpinVisual();
UniTask StopSpinVisualAsync(ISpinResult result);
```

**`ISpinProvider`** — abstrai a chamada de API:
```csharp
UniTask<bool> SpinAsync(int betPerLine, CancellationToken ct = default);
```

**`IGameModel`** — o que os estados leem do model:
```csharp
int Coins { get; }
int FreeSpinsRemaining { get; }
ISpinResult LastSpin { get; }
event Action<int> OnCoinsChanged;
```

**`ISpinResult`** — marcador para o resultado de um spin:
```csharp
public interface ISpinResult { }
// Cada jogo implementa com seu DTO concreto
```

### `SlotMachineConfig` (ScriptableObject)

Criado em **Assets → Create → Slot Engine → Machine Config**.

| Campo | Padrão | Descrição |
|---|---|---|
| `paylineCount` | 15 | Deve coincidir com `PAYLINES.length` no backend |
| `minBet` | 1 | Aposta mínima por linha |
| `maxBet` | 100 | Aposta máxima por linha |
| `minSpinDuration` | 1.5 | Duração mínima da animação de giro (segundos) |

### `AudioManager` (Singleton)

```csharp
AudioManager.Instance.Play("nome-do-audio");
AudioManager.Instance.Play("nome-do-audio", pitch: 1.2f);
AudioManager.Instance.Stop("nome-do-audio");
AudioManager.Instance.StopAll();
```

Configure os clips no Inspector do GameObject `AudioManager` na cena.

---

## 4. Unity — Jogo Slot Dogs

```
slot-dogs-unity/Assets/Scripts/Games/SlotDogs/
├── View/
│   ├── SlotMachineView.cs    # Implementa ISlotMachineView (UI, reels, partículas)
│   ├── ReelStrip.cs          # Animação de giro de uma coluna individual
│   ├── MenuView.cs           # UI da tela de menu (botão Play)
│   ├── SessionView.cs        # UI de auditoria (seed, rotate)
│   └── ISessionView.cs       # Contrato da SessionView
├── Presenter/
│   ├── SlotMachinePresenter.cs  # Orquestra View + StateMachine + Eventos
│   └── SessionPresenter.cs      # Singleton persistente; implementa ISpinProvider
├── Model/
│   └── SessionModel.cs       # Implementa IGameModel; estado da sessão
├── Network/
│   ├── ApiClient.cs          # Chamadas HTTP para o backend
│   └── Dtos.cs               # DTOs JSON (SpinResponse implementa ISpinResult)
├── Data/
│   └── SymbolLibrary.cs      # ScriptableObject: symbolId → prefab
└── Editor/
    └── SymbolLibraryEditor.cs  # Validação no Inspector
```

### Símbolos do Slot Dogs

| ID | Símbolo | Multiplicadores (3x / 4x / 5x) |
|---|---|---|
| 0 | Husky Siberiano | 35 / 75 / 500 |
| 1 | Golden Retriever | 15 / 40 / 200 |
| 2 | Shiba Inu | 12 / 35 / 175 |
| 3 | Pug | 9 / 22 / 90 |
| 4 | Beagle | 6 / 18 / 65 |
| 5 | Dachshund | 5 / 16 / 55 |
| 6 | Wild (Patinha) | Substitui qualquer símbolo |
| 7 | Scatter (Ossinho) | Ativa free spins (3+ scatters) |
| 8 | Blank | Sem pagamento |

---

## 5. Frontend

```
slot-dogs-frontend/
└── app/
    └── page.tsx   # Layout 16:9 com container queries + embed do build WebGL
```

### Iniciar

```powershell
cd slot-dogs-frontend
npm install
node node_modules\next\dist\bin\next dev --port 3001
```

O build WebGL do Unity é servido na porta 3001 dentro de um container 16:9 que se adapta a qualquer tamanho de janela.

---

## 6. Como implementar um novo jogo

Este guia descreve o mínimo necessário para criar um segundo jogo (ex.: **Slot Cats**) reutilizando toda a engine.

### 6.1 Backend — novo `config.js`

Copie `slot-dogs-backend/src/engine/config.js` e ajuste:

```js
// slot-cats-backend/src/engine/config.js
const SYMBOL_IDS = {
  SIAMESE: 0, PERSIAN: 1, TABBY: 2,
  WILD: 3, SCATTER: 4, BLANK: 5,
};

const PAYTABLE = {
  [SYMBOL_IDS.SIAMESE]: [30, 70, 400],
  // ...
};

const REEL_WEIGHTS = [
  [3, 4, 4, 5, 2, 2],  // reel 1
  // ... (um array por reel)
];

const PAYLINES = [
  { id: 1, name: 'Linha do meio', path: [1, 1, 1, 1, 1] },
  // ...
];
```

O `spinEngine.js` é reutilizável sem modificação — ele só lê `config.js`.

### 6.2 Unity — nova View

Crie `Assets/Scripts/Games/SlotCats/View/SlotCatsView.cs` implementando `ISlotMachineView`:

```csharp
public class SlotCatsView : MonoBehaviour, ISlotMachineView
{
    // Seus campos de UI (reels, textos, botões, partículas...)

    public void SetSpinInteractable(bool interactable) { /* ... */ }
    public void UpdateCoins(int coins)                  { /* ... */ }
    public void UpdateFreeSpins(int remaining)          { /* ... */ }
    public void SetAutoSpinActive(bool active)          { /* ... */ }
    public void StartSpinVisual()                       { /* ... */ }

    public async UniTask StopSpinVisualAsync(ISpinResult result)
    {
        // Cast para o DTO do seu jogo
        var response = (CatSpinResponse)result;
        // ... animações específicas do Slot Cats
    }

    // Eventos consumidos pelo Presenter
    public event Action OnSpinRequested;
    public event Action OnAutoSpinToggled;
    public event Action OnBetIncreaseRequested;
    public event Action OnBetDecreaseRequested;
}
```

### 6.3 Unity — novo SpinProvider

Crie `Assets/Scripts/Games/SlotCats/Network/SlotCatsSpinProvider.cs` implementando `ISpinProvider`:

```csharp
public class SlotCatsSpinProvider : MonoBehaviour, ISpinProvider
{
    private SlotCatsApiClient _api;
    private SlotCatsModel     _model;

    public async UniTask<bool> SpinAsync(int betPerLine, CancellationToken ct = default)
    {
        try
        {
            var response = await _api.SpinAsync(betPerLine, ct);
            _model.ApplySpin(response);   // atualiza Coins, FreeSpinsRemaining, LastSpin
            return true;
        }
        catch { return false; }
    }
}
```

### 6.4 Unity — novo Model

Crie `SlotCatsModel.cs` implementando `IGameModel`:

```csharp
public class SlotCatsModel : IGameModel
{
    public int     Coins              { get; private set; }
    public int     FreeSpinsRemaining { get; private set; }

    // DTO concreto do jogo
    public CatSpinResponse LastSpin   { get; private set; }

    // Implementação explícita para a engine
    ISpinResult IGameModel.LastSpin   => LastSpin;

    public event Action<int> OnCoinsChanged;

    public void ApplySpin(CatSpinResponse r)
    {
        LastSpin           = r;
        Coins              = r.session.coins;
        FreeSpinsRemaining = r.session.freeSpinsRemaining;
        OnCoinsChanged?.Invoke(Coins);
    }
}
```

### 6.5 Unity — novo DTO

O DTO do resultado do spin precisa implementar `ISpinResult`:

```csharp
[Serializable]
public class CatSpinResponse : ISpinResult
{
    public CatSpinData    spin;
    public CatSessionData session;
}
```

### 6.6 Unity — novo Presenter

Crie `SlotCatsPresenter.cs` herdando ou copiando `SlotMachinePresenter`. A única diferença é que você injeta sua View e SpinProvider:

```csharp
public class SlotCatsPresenter : MonoBehaviour
{
    [SerializeField] private SlotCatsView         _view;
    [SerializeField] private SlotCatsSpinProvider _spinProvider;
    [SerializeField] private SlotMachineConfig    _config;   // mesmo asset, reutilizável

    private void Start()
    {
        var model         = new SlotCatsModel();
        var stateMachine  = new SlotStateMachine();
        var ctx           = new SlotGameContext(
            _view, _spinProvider, model, stateMachine,
            _config.minBet, _config.maxBet,
            _config.paylineCount, _config.minSpinDuration);

        // Subscrever eventos da View...
        stateMachine.Transition(new IdleState(ctx));
    }
}
```

### 6.7 Unity — novo `SlotMachineConfig`

No Project window: **Assets → Create → Slot Engine → Machine Config**

Configure `paylineCount` de acordo com o backend do novo jogo.

### 6.8 Checklist de implementação

- [ ] Backend: novo `config.js` com símbolos, pesos e paylines
- [ ] DTO: nova classe implementando `ISpinResult`
- [ ] Model: nova classe implementando `IGameModel`
- [ ] SpinProvider: nova classe implementando `ISpinProvider`
- [ ] View: nova classe implementando `ISlotMachineView`
- [ ] Presenter: cria `SlotGameContext` com as dependências acima
- [ ] Asset `SlotMachineConfig` criado e atribuído no Inspector
- [ ] Símbolos configurados em um `SymbolLibrary` (ou equivalente)
- [ ] Cenas `MenuScene` e `GameScene` configuradas no Build Settings

---

## Dependências Unity

| Pacote | Versão | Uso |
|---|---|---|
| UniTask | ≥ 2.5 | `async/await` sem alocações (estados, animações) |
| DOTween | ≥ 1.2 | Tweens de UI (pulse, scale, fade) |
| TextMeshPro | built-in | Todos os textos de UI |
| Newtonsoft.Json | ≥ 3.0 | Deserialização de `int[][]` (grid de reels) |

---

## Convenções

- **Sem lógica de jogo no cliente** — o Unity é um renderer. Toda decisão de resultado vem do backend.
- **Engine independente de jogo** — nenhum arquivo em `Engine/` deve importar nada de `Games/`.
- **States são stateless por design** — todo estado mutável fica em `SlotGameContext` ou no Model.
- **Eventos, não polling** — o Model dispara `OnCoinsChanged`; o Presenter escuta e atualiza a View.
