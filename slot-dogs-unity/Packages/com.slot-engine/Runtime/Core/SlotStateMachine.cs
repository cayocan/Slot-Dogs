using System;
using UnityEngine;

namespace SlotEngine
{
/// <summary>
/// Máquina de estados genérica para slot machine.
/// Gerencia o ciclo Enter/Exit dos estados e expõe o estado atual.
/// Não conhece nenhum estado concreto — é completamente desacoplada.
/// </summary>
public class SlotStateMachine
{
    // ── Estado atual ──────────────────────────────────────────────────────────
    public ISlotState Current { get; private set; }

    /// <summary>Disparado após toda transição bem-sucedida.</summary>
    public event Action<ISlotState, ISlotState> OnStateChanged;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sai do estado atual e entra no próximo.
    /// Transições para null ou para o próprio estado atual são ignoradas.
    /// </summary>
    public void Transition(ISlotState next)
    {
        if (next == null)
        {
            Debug.LogWarning("[SlotStateMachine] Tentativa de transição para estado null ignorada.");
            return;
        }

        if (next == Current)
        {
            Debug.LogWarning($"[SlotStateMachine] Transição ignorada: já em '{Current?.Name}'.");
            return;
        }

        var prev = Current;

        Debug.Log($"[SlotStateMachine] {prev?.Name ?? "null"} → {next.Name}");

        Current?.Exit();
        Current = next;
        OnStateChanged?.Invoke(prev, next);
        Current.Enter();
    }

    /// <summary>Retorna true se o estado atual é do tipo T.</summary>
    public bool IsIn<T>() where T : ISlotState => Current is T;
}
}
