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
        _view.OnSpinRequested += OnSpinRequested;

        // Model → Presenter → View
        var model = sp.Model;
        model.OnSpinCompleted += OnSpinCompleted;
        model.OnCoinsChanged  += OnCoinsChanged;

        // Estado inicial da sessão já ativa
        _view.UpdateCoins(model.Coins);
        _view.UpdateFreeSpins(model.FreeSpinsRemaining);
    }

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnSpinRequested -= OnSpinRequested;

        var model = SessionPresenter.Instance?.Model;
        if (model != null)
        {
            model.OnSpinCompleted -= OnSpinCompleted;
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
        _view.SetSpinInteractable(false);
        await SessionPresenter.Instance.RequestSpinAsync();
        _view.SetSpinInteractable(true);
    }

    private void OnSpinCompleted(SpinResponse result)
    {
        _view.Populate(result);

        if (result?.session != null)
            _view.UpdateFreeSpins(result.session.freeSpinsRemaining);
    }

    private void OnCoinsChanged(int coins)
    {
        _view.UpdateCoins(coins);
    }
}
