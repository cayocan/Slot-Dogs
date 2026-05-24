using Cysharp.Threading.Tasks;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SlotMachinePresenter
//  Presenter puro: sem campos de UI, sem TMP_Text, sem Button.
//  Responsabilidades:
//    • Assinar eventos do SessionModel (OnSpinCompleted, OnCoinsChanged)
//    • Assinar o evento OnSpinRequested da SlotMachineView
//    • Orquestrar a chamada a SessionPresenter.RequestSpinAsync()
//    • Comandar a SlotMachineView via métodos públicos
//
//  Setup na cena (GameScene):
//    1. Adicione este script a qualquer GameObject (ex.: "GameManager").
//    2. Conecte _view via Inspector (o GO que tem SlotMachineView).
//
//  Pré-requisito: SessionPresenter.Instance deve existir (criado na MenuScene).
// ═══════════════════════════════════════════════════════════════════════════════

public class SlotMachinePresenter : MonoBehaviour
{
    [SerializeField] private SlotMachineView _view;

    // Deve bater com PAYLINES.length no backend (config.js)
    private const int PaylineCount = 15;

    [SerializeField] private int _minBet = 1;
    [SerializeField] private int _maxBet = 100;
    [SerializeField] private float _minSpinDuration = 1.5f;

    private int  _betPerLine;
    private bool _isSpinning;

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

        // View → Presenter
        _view.OnSpinRequested      += OnSpinRequested;
        _view.OnBetIncreaseRequested += OnBetIncrease;
        _view.OnBetDecreaseRequested += OnBetDecrease;

        // Model → Presenter → View
        var model = sp.Model;
        model.OnCoinsChanged  += OnCoinsChanged;

        // Estado inicial
        _betPerLine = Mathf.Clamp(model.BetPerLine, _minBet, _maxBet);
        _view.UpdateCoins(model.Coins);
        _view.UpdateFreeSpins(model.FreeSpinsRemaining);
        _view.UpdateBetPerLine(_betPerLine, _minBet, _maxBet);
        RefreshSpinButton(model);
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnSpinRequested        -= OnSpinRequested;
            _view.OnBetIncreaseRequested -= OnBetIncrease;
            _view.OnBetDecreaseRequested -= OnBetDecrease;
        }

        var model = SessionPresenter.Instance?.Model;
        if (model != null)
        {
            model.OnCoinsChanged  -= OnCoinsChanged;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Handlers
    // ═════════════════════════════════════════════════════════════════════════

    private void OnSpinRequested()
    {
        SpinAsync().Forget();
    }

    private async UniTaskVoid SpinAsync()
    {
        _isSpinning = true;
        _view.SetSpinInteractable(false);
        _view.StartSpinVisual();

        try
        {
            // Chama API e garante duração mínima de animação em paralelo
            await UniTask.WhenAll(
                SessionPresenter.Instance.RequestSpinAsync(_betPerLine),
                UniTask.Delay((int)(_minSpinDuration * 1000f))
            );

            var model = SessionPresenter.Instance.Model;
            if (model.LastSpin != null)
            {
                await _view.StopSpinVisualAsync(model.LastSpin);
                _view.UpdateFreeSpins(model.FreeSpinsRemaining);
            }
        }
        finally
        {
            _isSpinning = false;
            RefreshSpinButton(SessionPresenter.Instance.Model);
        }
    }

    private void OnBetIncrease()
    {
        if (_isSpinning) return;
        _betPerLine = Mathf.Min(_betPerLine + 1, _maxBet);
        _view.UpdateBetPerLine(_betPerLine, _minBet, _maxBet);
        RefreshSpinButton(SessionPresenter.Instance.Model);
    }

    private void OnBetDecrease()
    {
        if (_isSpinning) return;
        _betPerLine = Mathf.Max(_betPerLine - 1, _minBet);
        _view.UpdateBetPerLine(_betPerLine, _minBet, _maxBet);
        RefreshSpinButton(SessionPresenter.Instance.Model);
    }

    private void OnCoinsChanged(int coins)
    {
        _view.UpdateCoins(coins);
        if (!_isSpinning)
            RefreshSpinButton(SessionPresenter.Instance.Model);
    }

    // ── Utilitário ────────────────────────────────────────────────────────────

    private void RefreshSpinButton(SessionModel model)
    {
        int totalBet = _betPerLine * PaylineCount;
        bool canSpin = model.FreeSpinsRemaining > 0 || model.Coins >= totalBet;
        _view.SetSpinInteractable(canSpin);
    }
}
