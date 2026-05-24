/// <summary>
/// Estado ocioso — jogador pode alterar a aposta e clicar em Spin.
/// Habilita/desabilita o botão Spin conforme saldo e aposta configurada.
/// </summary>
public class IdleState : SlotStateBase
{
    public override string Name => "Idle";

    public IdleState(SlotGameContext ctx) : base(ctx) { }

    public override void Enter()
    {
        RefreshSpinButton();
    }
}
