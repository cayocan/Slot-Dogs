using Cysharp.Threading.Tasks;

namespace SlotEngine
{
/// <summary>
/// Contrato da View da slot machine.
/// Os estados da engine dependem apenas desta interface — nunca da implementação concreta.
/// Ao criar um novo jogo, implemente esta interface na View específica do jogo.
/// </summary>
public interface ISlotMachineView
{
    /// <summary>Habilita ou desabilita o botão Spin.</summary>
    void SetSpinInteractable(bool interactable);

    /// <summary>Atualiza o display de moedas.</summary>
    void UpdateCoins(int coins);

    /// <summary>Atualiza o display de free spins (oculta quando zero).</summary>
    void UpdateFreeSpins(int remaining);

    /// <summary>Ativa ou desativa o indicador visual de auto-spin.</summary>
    void SetAutoSpinActive(bool active);

    /// <summary>Inicia a animação de giro em todos os reels.</summary>
    void StartSpinVisual();

    /// <summary>Para os reels com o resultado e executa as animações de vitória.</summary>
    UniTask StopSpinVisualAsync(ISpinResult result);
}
}
