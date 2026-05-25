using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Estado ocioso — jogador pode alterar a aposta e clicar em Spin.
/// Habilita/desabilita o botão Spin conforme saldo e aposta configurada.
/// Se auto-spin estiver ativo, dispara a próxima rodada após breve pausa.
/// </summary>
public class IdleState : SlotStateBase
{
    public override string Name => "Idle";

    private CancellationTokenSource _cts;

    public IdleState(SlotGameContext ctx) : base(ctx) { }

    public override void Enter()
    {
        RefreshSpinButton();

        if (!Ctx.IsAutoSpinActive) return;

        if (Ctx.CanSpin)
        {
            _cts = new CancellationTokenSource();
            AutoSpinAsync(_cts.Token).Forget();
        }
        else
        {
            // Saldo insuficiente: desativa auto-spin
            Ctx.IsAutoSpinActive = false;
            Ctx.View.SetAutoSpinActive(false);
        }
    }

    public override void Exit()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid AutoSpinAsync(CancellationToken ct)
    {
        // Pequena pausa para o jogador perceber o resultado antes da próxima rodada
        await UniTask.Delay(500, cancellationToken: ct).SuppressCancellationThrow();
        if (!ct.IsCancellationRequested && Ctx.IsAutoSpinActive && Ctx.CanSpin)
            Ctx.StateMachine.Transition(new SpinningState(Ctx));
    }
}
