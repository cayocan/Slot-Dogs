using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SlotMachinePresenter
//  Responsabilidades:
//    • Criar o SlotGameContext e a SlotStateMachine
//    • Delegar eventos da View e do Model para a SM
//    • Gerenciar aposta por linha (fora dos estados, pois é UI pura)
//
//  Setup na cena (GameScene):
//    1. Adicione este script a qualquer GameObject (ex.: "GameManager").
//    2. Conecte _view via Inspector.
//
//  Pré-requisito: SessionPresenter.Instance deve existir (MenuScene).
// ═══════════════════════════════════════════════════════════════════════════════

public class SlotMachinePresenter : MonoBehaviour
{
    [SerializeField] private SlotMachineView _view;

    [Tooltip("Deve coincidir com PAYLINES.length no backend (config.js)")]
    [SerializeField] private int   _paylineCount    = 15;
    [SerializeField] private int   _minBet          = 1;
    [SerializeField] private int   _maxBet          = 100;
    [SerializeField] private float _minSpinDuration = 1.5f;

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

        var model = sp.Model;

        _stateMachine = new SlotStateMachine();
        _ctx = new SlotGameContext(
            _view, model, _stateMachine,
            _minBet, _maxBet, _paylineCount, _minSpinDuration);

        _ctx.BetPerLine = Mathf.Clamp(model.BetPerLine, _minBet, _maxBet);

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
        _view.UpdateBetPerLine(_ctx.BetPerLine, _minBet, _maxBet);

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
        _ctx.BetPerLine = Mathf.Min(_ctx.BetPerLine + 1, _maxBet);
        _view.UpdateBetPerLine(_ctx.BetPerLine, _minBet, _maxBet);
        _view.SetSpinInteractable(_ctx.CanSpin);
    }

    private void OnBetDecrease()
    {
        if (!_stateMachine.IsIn<IdleState>()) return;
        _ctx.BetPerLine = Mathf.Max(_ctx.BetPerLine - 1, _minBet);
        _view.UpdateBetPerLine(_ctx.BetPerLine, _minBet, _maxBet);
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
