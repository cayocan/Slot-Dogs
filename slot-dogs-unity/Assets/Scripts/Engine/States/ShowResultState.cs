using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Estado pós-giro — atualiza saldo com valor real do servidor e
/// decide o próximo estado: FreeSpins (se houver) ou Idle.
/// </summary>
public class ShowResultState : SlotStateBase
{
    public override string Name => "ShowResult";

    private readonly ISpinResult         _result;
    private          CancellationTokenSource _cts;

    public ShowResultState(SlotGameContext ctx, ISpinResult result) : base(ctx)
    {
        _result = result;
    }

    public override void Enter()
    {
        _cts = new CancellationTokenSource();

        // Valor real do servidor (dedução + ganho já processados)
        Ctx.View.UpdateCoins(Ctx.Model.Coins);
        Ctx.View.UpdateFreeSpins(Ctx.Model.FreeSpinsRemaining);

        if (Ctx.Model.FreeSpinsRemaining > 0)
            // Pausa breve antes de iniciar free spins automaticamente
            TransitionAfterDelayAsync(new FreeSpinsState(Ctx), 1500, _cts.Token).Forget();
        else
            Ctx.StateMachine.Transition(new IdleState(Ctx));
    }

    public override void Exit()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async UniTaskVoid TransitionAfterDelayAsync(ISlotState next, int delayMs, CancellationToken ct)
    {
        await UniTask.Delay(delayMs, cancellationToken: ct).SuppressCancellationThrow();
        if (!ct.IsCancellationRequested)
            Ctx.StateMachine.Transition(next);
    }
}
