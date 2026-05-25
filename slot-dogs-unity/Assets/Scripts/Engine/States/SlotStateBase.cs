/// <summary>
/// Classe base para todos os estados da slot machine.
/// Injeta o contexto compartilhado e fornece helpers comuns.
/// </summary>
public abstract class SlotStateBase : ISlotState
{
    public abstract string Name { get; }

    protected SlotGameContext Ctx { get; }

    protected SlotStateBase(SlotGameContext ctx) => Ctx = ctx;

    public virtual void Enter() { }
    public virtual void Exit()  { }

    // ── Helpers compartilhados ────────────────────────────────────────────────

    /// <summary>Atualiza o botão de spin de acordo com saldo e aposta atual.</summary>
    protected void RefreshSpinButton()
    {
        Ctx.View.SetSpinInteractable(Ctx.CanSpin);
    }
}
