using UnityEngine;
using SlotEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Machine Presenter (Engine)
//  Responsabilidades:
//    • Criar o SlotGameContext e a SlotStateMachine
//    • Delegar eventos da View e do Model para a SM
//    • Gerenciar aposta por linha (fora dos estados, pois é UI pura)
//
//  Setup na cena (GameScene):
//    1. Adicione este script a qualquer GameObject (ex.: "GameManager").
//    2. Conecte _view (SlotMachineView) via Inspector.
//    3. Crie um asset SlotMachineConfig (Assets → Create → Slot Engine → Machine Config)
//       e atribua ao campo _config via Inspector.
//
//  Pré-requisito: SessionPresenter.Instance deve existir (MenuScene).
// ═══════════════════════════════════════════════════════════════════════════════

public class SlotMachinePresenter : MonoBehaviour
{
    [SerializeField] private SlotMachineView   _view;
    [SerializeField] private SlotMachineConfig _config;

    private SlotStateMachine _stateMachine;
    private SlotGameContext  _ctx;

    // ═════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida Unity
    // ═════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        var sp = SessionPresenter.Instance;
        if (sp == null)
        {
            Debug.LogError("[SlotMachinePresenter] SessionPresenter.Instance é null. " +
                "Certifique-se de que a MenuScene foi carregada antes da GameScene.");
            return;
        }

        if (_config == null)
        {
            Debug.LogError("[SlotMachinePresenter] SlotMachineConfig não atribuído no Inspector.");
            return;
        }

        var model = sp.Model;

        _stateMachine = new SlotStateMachine();
        _ctx = new SlotGameContext(
            _view, sp, model, _stateMachine,
            _config.minBet, _config.maxBet, _config.paylineCount, _config.minSpinDuration);

        _ctx.BetPerLine = Mathf.Clamp(model.BetPerLine, _config.minBet, _config.maxBet);

        // View → SM
        _view.OnSpinRequested        += OnSpinRequested;
        _view.OnAutoSpinToggled      += OnAutoSpinToggled;
        _view.OnBetIncreaseRequested += OnBetIncrease;
        _view.OnBetDecreaseRequested += OnBetDecrease;

        // Model → View (somente quando fora de um spin ativo)
        model.OnCoinsChanged += OnCoinsChanged;

        // Estado inicial da UI
        _view.UpdateCoins(_ctx.Model.Coins);
        _view.UpdateFreeSpins(model.FreeSpinsRemaining);
        _view.UpdateBetPerLine(_ctx.BetPerLine, _config.minBet, _config.maxBet, _ctx.TotalBet);

        // Inicia a máquina de estados — sem isso Current permanece null e nenhum spin dispara
        _stateMachine.Transition(new IdleState(_ctx));
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnSpinRequested        -= OnSpinRequested;
            _view.OnAutoSpinToggled      -= OnAutoSpinToggled;
            _view.OnBetIncreaseRequested -= OnBetIncrease;
            _view.OnBetDecreaseRequested -= OnBetDecrease;
        }

        var model = SessionPresenter.Instance?.Model;
        if (model != null)
            model.OnCoinsChanged -= OnCoinsChanged;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Handlers
    // ═════════════════════════════════════════════════════════════════════════

    private void OnSpinRequested()
    {
        // Aceita spin apenas em Idle; estados ativos ignoram
        if (_stateMachine.IsIn<IdleState>())
            _stateMachine.Transition(new SpinningState(_ctx));
    }

    private void OnAutoSpinToggled()
    {
        _ctx.IsAutoSpinActive = !_ctx.IsAutoSpinActive;
        _view.SetAutoSpinActive(_ctx.IsAutoSpinActive);

        // Se ativado enquanto ocioso e há saldo: inicia imediatamente
        if (_ctx.IsAutoSpinActive && _stateMachine.IsIn<IdleState>() && _ctx.CanSpin)
            _stateMachine.Transition(new SpinningState(_ctx));
    }

    private void OnBetIncrease()
    {
        if (!_stateMachine.IsIn<IdleState>()) return;
        _ctx.BetPerLine = Mathf.Min(_ctx.BetPerLine + 1, _config.maxBet);
        _view.UpdateBetPerLine(_ctx.BetPerLine, _config.minBet, _config.maxBet, _ctx.TotalBet);
        _view.SetSpinInteractable(_ctx.CanSpin);
    }

    private void OnBetDecrease()
    {
        if (!_stateMachine.IsIn<IdleState>()) return;
        _ctx.BetPerLine = Mathf.Max(_ctx.BetPerLine - 1, _config.minBet);
        _view.UpdateBetPerLine(_ctx.BetPerLine, _config.minBet, _config.maxBet, _ctx.TotalBet);
        _view.SetSpinInteractable(_ctx.CanSpin);
    }

    private void OnCoinsChanged(int coins)
    {
        // Estados ativos gerenciam o display de moedas; só atualiza em Idle
        if (_stateMachine.IsIn<IdleState>())
        {
            _view.UpdateCoins(coins);
            _view.SetSpinInteractable(_ctx.CanSpin);
        }
    }
}
