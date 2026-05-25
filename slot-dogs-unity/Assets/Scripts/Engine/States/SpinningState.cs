using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Estado de giro — cobre todo o ciclo:
///   1. Deduz aposta imediatamente no display
///   2. Chama a API em paralelo com a duração mínima de animação
///   3. Para a animação com o resultado
///   4. Transiciona para ShowResultState
/// </summary>
public class SpinningState : SlotStateBase
{
    public override string Name => "Spinning";

    private CancellationTokenSource _cts;

    public SpinningState(SlotGameContext ctx) : base(ctx) { }

    public override void Enter()
    {
        _cts = new CancellationTokenSource();

        Ctx.View.SetSpinInteractable(false);

        // Exibe dedução imediata antes da API confirmar
        Ctx.View.UpdateCoins(Ctx.Model.Coins - Ctx.TotalBet);
        Ctx.View.StartSpinVisual();

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
            // API + duração mínima em paralelo (API não é cancelável por design)
            await UniTask.WhenAll(
                Ctx.SpinProvider.SpinAsync(Ctx.BetPerLine, ct),
                UniTask.Delay((int)(Ctx.MinSpinDuration * 1000f), cancellationToken: ct)
            );

            ct.ThrowIfCancellationRequested();

            var lastSpin = Ctx.Model.LastSpin;
            if (lastSpin != null)
                await Ctx.View.StopSpinVisualAsync(lastSpin);

            ct.ThrowIfCancellationRequested();

            Ctx.StateMachine.Transition(new ShowResultState(Ctx, lastSpin));
        }
        catch (OperationCanceledException)
        {
            // Estado foi cancelado externamente — sem transição
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpinningState] Erro inesperado: {ex.Message}");
            // Recupera para Idle mesmo em caso de falha
            Ctx.StateMachine.Transition(new IdleState(Ctx));
        }
    }
}
