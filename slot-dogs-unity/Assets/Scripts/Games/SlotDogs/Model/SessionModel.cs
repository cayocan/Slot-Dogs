using System;
using System.Security.Cryptography;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SessionModel
//  Armazena o estado completo da sessão de jogo.
//  Classe POCO pura (sem dependência de Unity/Mono) — testável isoladamente.
//
//  Atualizado por SessionPresenter após cada resposta da API:
//    Apply(SessionStateResponse)  → criação / consulta de sessão
//    ApplySpin(SpinResponse)      → resultado de spin
//    ApplyClientSeed(string)      → confirmação de seed do jogador
//    ApplyRotate(RotateResponse)  → rotação de server seed
// ═══════════════════════════════════════════════════════════════════════════════

public class SessionModel : IGameModel
{
    // ── Estado da sessão ──────────────────────────────────────────────────────

    /// <summary>ID único da sessão retornado pelo backend.</summary>
    public string SessionId          { get; private set; }

    /// <summary>SHA-256 do server seed ativo (o seed real só é revelado após rotate).</summary>
    public string ServerSeedHash     { get; private set; }

    /// <summary>Client seed definido pelo jogador. null até ser configurado.</summary>
    public string ClientSeed         { get; private set; }

    /// <summary>Saldo atual de moedas.</summary>
    public int    Coins              { get; private set; }

    /// <summary>Aposta por linha configurada na sessão.</summary>
    public int    BetPerLine         { get; private set; }

    /// <summary>Número de spins realizados nessa sessão (incrementado a cada spin).</summary>
    public int    Nonce              { get; private set; }

    /// <summary>Quantidade de free spins ainda disponíveis.</summary>
    public int    FreeSpinsRemaining { get; private set; }

    // ── Estado do último spin ─────────────────────────────────────────────────

    /// <summary>Resposta completa do último POST /spin. null antes do primeiro spin.</summary>
    public SpinResponse LastSpin     { get; private set; }

    // Implementação explícita de IGameModel — permite que a engine leia LastSpin
    // sem depender do tipo concreto SpinResponse.
    ISpinResult IGameModel.LastSpin  => LastSpin;

    // ── Estado da última rotação ──────────────────────────────────────────────

    /// <summary>Dados revelados na última rotação de seed. null enquanto não houver rotate.</summary>
    public RotateRevealedData LastRevealedSeed { get; private set; }

    // ── Propriedades derivadas ────────────────────────────────────────────────

    /// <summary>true após a primeira chamada a Apply() com sucesso.</summary>
    public bool HasSession => !string.IsNullOrEmpty(SessionId);

    /// <summary>true se o jogador definiu um client seed.</summary>
    public bool HasClientSeed => !string.IsNullOrEmpty(ClientSeed);

    // ── Eventos ───────────────────────────────────────────────────────────────

    /// <summary>Disparado após qualquer atualização de estado.</summary>
    public event Action OnChanged;

    /// <summary>Disparado especificamente quando o saldo de moedas muda.</summary>
    public event Action<int> OnCoinsChanged;

    /// <summary>Disparado após um spin ser processado.</summary>
    public event Action<SpinResponse> OnSpinCompleted;

    /// <summary>Disparado após a rotação de seed, expondo os dados revelados.</summary>
    public event Action<RotateResponse> OnSeedRotated;

    // ── Métodos de atualização ────────────────────────────────────────────────

    /// <summary>
    /// Aplica o estado recebido de POST /session (201) ou GET /session/:id (200).
    /// </summary>
    public void Apply(SessionStateResponse r)
    {
        if (r == null) throw new ArgumentNullException(nameof(r));

        SessionId          = r.sessionId;
        ServerSeedHash     = r.serverSeedHash;
        ClientSeed         = r.clientSeed;
        Coins              = r.coins;
        BetPerLine         = r.betPerLine;
        Nonce              = r.nonce;
        FreeSpinsRemaining = r.freeSpinsRemaining;

        OnChanged?.Invoke();
    }

    /// <summary>
    /// Aplica o resultado de POST /spin (200), atualizando saldo, nonce e free spins.
    /// </summary>
    public void ApplySpin(SpinResponse r)
    {
        if (r == null) throw new ArgumentNullException(nameof(r));

        LastSpin = r;

        if (r.session != null)
        {
            int previousCoins = Coins;

            Coins              = r.session.coins;
            Nonce              = r.session.nonce;
            FreeSpinsRemaining = r.session.freeSpinsRemaining;
            ServerSeedHash     = r.session.serverSeedHash;

            if (Coins != previousCoins)
                OnCoinsChanged?.Invoke(Coins);
        }

        OnSpinCompleted?.Invoke(r);
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Confirma que o client seed foi aceito pelo backend (POST /session/:id/seed).
    /// </summary>
    public void ApplyClientSeed(string seed)
    {
        ClientSeed = seed;
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Aplica o resultado de POST /session/:id/rotate, armazenando o seed revelado
    /// e atualizando o hash do novo seed ativo.
    /// </summary>
    public void ApplyRotate(RotateResponse r)
    {
        if (r == null) throw new ArgumentNullException(nameof(r));

        LastRevealedSeed = r.revealed;
        ServerSeedHash   = r.newServerSeedHash;

        OnSeedRotated?.Invoke(r);
        OnChanged?.Invoke();
    }

    // ── Geração de seed ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gera um client seed criptograficamente aleatório (16 bytes → 32 chars hex),
    /// armazena em <see cref="ClientSeed"/> e dispara <see cref="OnChanged"/>.
    /// Retorna o seed gerado para que o Presenter possa enviá-lo à API.
    /// </summary>
    public string GenerateClientSeed()
    {
        byte[] bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        ClientSeed = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        OnChanged?.Invoke();
        return ClientSeed;
    }

    /// <summary>
    /// Retorna o client seed atual.
    /// null enquanto <see cref="GenerateClientSeed"/> ou <see cref="ApplyClientSeed"/> não forem chamados.
    /// </summary>
    public string GetClientSeed() => ClientSeed;

    /// <summary>
    /// Reseta o modelo para o estado inicial (útil para logout / nova sessão).
    /// </summary>
    public void Reset()
    {
        SessionId          = null;
        ServerSeedHash     = null;
        ClientSeed         = null;
        Coins              = 0;
        BetPerLine         = 0;
        Nonce              = 0;
        FreeSpinsRemaining = 0;
        LastSpin           = null;
        LastRevealedSeed   = null;

        OnChanged?.Invoke();
    }
}
