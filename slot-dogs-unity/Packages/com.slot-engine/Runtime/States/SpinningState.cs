using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotEngine
{
/// <summary>
/// Estado de giro — cobre todo o ciclo:
///   1. Deduz aposta imediatamente no display
///   2. Chama a API em paralelo com a duração mínima de animação
///   3. Para a animação com o resultado
///   4. Transiciona para ShowResultState
/// </summary>
public class SpinningState : ISlotState
{
    public string Name => "Spinning";

    protected SlotGameContext Ctx { get; }

    private CancellationTokenSource _cts;

    public SpinningState(SlotGameContext ctx) { Ctx = ctx; }

    public void Enter()
    {
        _cts = new CancellationTokenSource();

        Ctx.View.SetSpinInteractable(false);

        // Exibe dedução imediata antes da API confirmar
        Ctx.View.UpdateCoins(Ctx.Model.Coins - Ctx.TotalBet);
        Ctx.View.StartSpinVisual();

        RunAsync(_cts.Token).Forget();
    }

    public void Exit()
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
            // Inicia o delay mínimo de animação e aguarda a API em paralelo
            var minDelayTask = UniTask.Delay((int)(Ctx.MinSpinDuration * 1000f), cancellationToken: ct);
            var spinOk       = await Ctx.SpinProvider.SpinAsync(Ctx.BetPerLine, ct);
            await minDelayTask;

            ct.ThrowIfCancellationRequested();

            if (!spinOk)
            {
                // API falhou (ex.: saldo insuficiente) — para os visuais, desativa auto-spin e volta ao Idle
                await Ctx.View.StopSpinVisualAsync(null);
                Ctx.IsAutoSpinActive = false;
                Ctx.View.SetAutoSpinActive(false);
                Ctx.View.UpdateCoins(Ctx.Model.Coins);
                Ctx.StateMachine.Transition(new IdleState(Ctx));
                return;
            }

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
}
