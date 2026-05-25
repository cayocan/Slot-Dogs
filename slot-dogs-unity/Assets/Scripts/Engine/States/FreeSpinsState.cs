using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Estado de free spins — executa rodadas automáticas sem custo para o jogador
/// até que <see cref="SessionModel.FreeSpinsRemaining"/> chegue a zero.
/// </summary>
public class FreeSpinsState : SlotStateBase
{
    public override string Name => "FreeSpins";

    private CancellationTokenSource _cts;

    /// <summary>Pausa em ms entre cada free spin para o jogador perceber o resultado.</summary>
    private const int BetweenSpinsDelayMs = 1000;

    public FreeSpinsState(SlotGameContext ctx) : base(ctx) { }

    public override void Enter()
    {
        _cts = new CancellationTokenSource();

        Ctx.View.SetSpinInteractable(false);
        RunAsync(_cts.Token).Forget();
    }

    public override void Exit()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async UniTaskVoid RunAsync(CancellationToken ct)
    {
        try
        {
            while (Ctx.Model.FreeSpinsRemaining > 0 && !ct.IsCancellationRequested)
            {
                Ctx.View.StartSpinVisual();

                await UniTask.WhenAll(
                    Ctx.SpinProvider.SpinAsync(Ctx.BetPerLine, ct),
                    UniTask.Delay((int)(Ctx.MinSpinDuration * 1000f), cancellationToken: ct)
                );

                ct.ThrowIfCancellationRequested();

                var lastSpin = Ctx.Model.LastSpin;
                if (lastSpin != null)
                    await Ctx.View.StopSpinVisualAsync(lastSpin);

                // Atualiza display após cada rodada gratuita
                Ctx.View.UpdateCoins(Ctx.Model.Coins);
                Ctx.View.UpdateFreeSpins(Ctx.Model.FreeSpinsRemaining);

                ct.ThrowIfCancellationRequested();

                // Pausa entre rodadas (exceto após a última)
                if (Ctx.Model.FreeSpinsRemaining > 0)
                    await UniTask.Delay(BetweenSpinsDelayMs, cancellationToken: ct);
            }

            ct.ThrowIfCancellationRequested();
            Ctx.StateMachine.Transition(new IdleState(Ctx));
        }
        catch (OperationCanceledException)
        {
            // Estado cancelado externamente — sem transição
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FreeSpinsState] Erro inesperado: {ex.Message}");
            Ctx.StateMachine.Transition(new IdleState(Ctx));
        }
    }
}
