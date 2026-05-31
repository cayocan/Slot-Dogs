namespace SlotEngine
{
/// <summary>
/// Contexto compartilhado entre todos os estados da slot machine.
/// Depende apenas de interfaces — nenhuma referência a implementações concretas de jogo.
/// Para um novo jogo: passe uma <see cref="ISlotMachineView"/> e um <see cref="ISpinProvider"/> diferentes.
/// </summary>
public class SlotGameContext
{
    // ── Referências (interfaces — sem acoplamento a implementações concretas) ──
    public ISlotMachineView View            { get; }
    public ISpinProvider    SpinProvider    { get; }
    public IGameModel       Model           { get; }
    public SlotStateMachine StateMachine    { get; }

    // ── Configuração de jogo ──────────────────────────────────────────────────
    public int   MinBet            { get; }
    public int   MaxBet            { get; }
    public int   PaylineCount      { get; }
    public float MinSpinDuration   { get; }  // segundos

    // ── Estado mutável de aposta ──────────────────────────────────────────────
    public int   BetPerLine        { get; set; }

    // ── Estado mutável de auto-spin ───────────────────────────────────────────
    public bool  IsAutoSpinActive  { get; set; }

    // ── Derivados ─────────────────────────────────────────────────────────────
    public int   TotalBet          => BetPerLine * PaylineCount;
    public bool  CanSpin           => Model.FreeSpinsRemaining > 0
                                   || Model.Coins >= TotalBet;

    /// <summary>
    /// Atualiza o estado do botão de spin na View conforme o estado atual do contexto.
    /// Centraliza a lógica de UI relacionada ao botão de spin no contexto compartilhado.
    /// </summary>
    public void RefreshSpinButton()
    {
        View.SetSpinInteractable(CanSpin);
    }

    // ─────────────────────────────────────────────────────────────────────────

    public SlotGameContext(
        ISlotMachineView view,
        ISpinProvider    spinProvider,
        IGameModel       model,
        SlotStateMachine stateMachine,
        int              minBet,
        int              maxBet,
        int              paylineCount,
        float            minSpinDuration)
    {
        View            = view;
        SpinProvider    = spinProvider;
        Model           = model;
        StateMachine    = stateMachine;
        MinBet          = minBet;
        MaxBet          = maxBet;
        PaylineCount    = paylineCount;
        MinSpinDuration = minSpinDuration;
        BetPerLine      = minBet;
    }
}
}
