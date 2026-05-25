// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — ISessionView
//  Contrato que toda View de sessão deve implementar.
//  O SessionPresenter depende apenas desta interface, nunca da View concreta,
//  permitindo trocar a UI (UI Toolkit, uGUI, etc.) sem alterar o Presenter.
// ═══════════════════════════════════════════════════════════════════════════════

public interface ISessionView
{
    // ── Feedback de carregamento ──────────────────────────────────────────────

    /// <summary>Exibe ou oculta indicador de loading (spinner, overlay, etc.).</summary>
    void ShowLoading(bool isLoading);

    // ── Atualização de estado ─────────────────────────────────────────────────

    /// <summary>Atualiza o display de moedas.</summary>
    void UpdateCoins(int coins);

    /// <summary>Atualiza o display de free spins restantes.</summary>
    void UpdateFreeSpins(int freeSpinsRemaining);

    /// <summary>Atualiza o hash do server seed exibido ao jogador (auditoria).</summary>
    void UpdateServerSeedHash(string hash);

    /// <summary>Atualiza o client seed exibido (confirma que foi aceito pelo backend).</summary>
    void UpdateClientSeed(string seed);

    // ── Resultado de spin ─────────────────────────────────────────────────────

    /// <summary>
    /// Entrega o resultado completo de um spin para a View animar reels,
    /// exibir vitórias e atualizar a UI de forma independente do Presenter.
    /// </summary>
    void ShowSpinResult(SpinResponse result);

    // ── Rotação de seed ───────────────────────────────────────────────────────

    /// <summary>
    /// Entrega os dados revelados após um rotate para a View exibir
    /// o server seed real (provably fair) e o range de nonces cobertos.
    /// </summary>
    void ShowRotateResult(RotateResponse result);

    // ── Erros ─────────────────────────────────────────────────────────────────

    /// <summary>Exibe mensagem de erro amigável ao jogador.</summary>
    void ShowError(string message);
}
