/// <summary>
/// Contexto compartilhado entre todos os estados da slot machine.
/// Imutável em estrutura — só BetPerLine é mutável pelo presenter.
/// Genérico o suficiente para reutilização em qualquer slot com MVP + UniTask.
/// </summary>
public class SlotGameContext
{
    // ── Referências ───────────────────────────────────────────────────────────
    public SlotMachineView  View            { get; }
    public SessionModel     Model           { get; }
    public SlotStateMachine StateMachine    { get; }

    // ── Configuração de jogo ──────────────────────────────────────────────────
    public int   MinBet            { get; }
    public int   MaxBet            { get; }
    public int   PaylineCount      { get; }
    public float MinSpinDuration   { get; }  // segundos

    // ── Estado mutável de aposta ──────────────────────────────────────────────
    public int   BetPerLine        { get; set; }

    // ── Derivados ─────────────────────────────────────────────────────────────
    public int   TotalBet          => BetPerLine * PaylineCount;
    public bool  CanSpin           => Model.FreeSpinsRemaining > 0
                                   || Model.Coins >= TotalBet;

    // ─────────────────────────────────────────────────────────────────────────

    public SlotGameContext(
        SlotMachineView  view,
        SessionModel     model,
        SlotStateMachine stateMachine,
        int              minBet,
        int              maxBet,
        int              paylineCount,
        float            minSpinDuration)
    {
        View           = view;
        Model          = model;
        StateMachine   = stateMachine;
        MinBet         = minBet;
        MaxBet         = maxBet;
        PaylineCount   = paylineCount;
        MinSpinDuration = minSpinDuration;
        BetPerLine     = minBet;
    }
}
