using System;
using SlotEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — DTOs de serialização JSON
//  Mapeiam exatamente os contratos da API REST (slot-dogs-backend).
//  Requer Newtonsoft.Json (com.unity.nuget.newtonsoft-json) para int[][]
//  no campo SpinData.grid. Se usar JsonUtility, substitua int[][] por GridRow[].
// ═══════════════════════════════════════════════════════════════════════════════

// ─── Compartilhado ────────────────────────────────────────────────────────────

/// <summary>Resposta de erro genérica retornada em todos os endpoints (4xx).</summary>
[Serializable]
public class ErrorResponse
{
    public string error;
}

// ─── Session ─────────────────────────────────────────────────────────────────

/// <summary>
/// Estado da sessão — retornado por:
///   POST /session (201),
///   GET  /session/:id (200).
/// </summary>
[Serializable]
public class SessionStateResponse
{
    public string sessionId;
    /// <summary>SHA-256 do server seed ativo (seed real nunca é exposto antes do rotate).</summary>
    public string serverSeedHash;
    public int coins;
    public int betPerLine;
    public int nonce;
    public int freeSpinsRemaining;
    /// <summary>null enquanto o jogador não chamar POST /session/:id/seed.</summary>
    public string clientSeed;
}

/// <summary>Body de POST /session/:id/seed</summary>
[Serializable]
public class SetClientSeedRequest
{
    /// <summary>Mínimo 8 caracteres.</summary>
    public string clientSeed;
}

/// <summary>Resposta de POST /session/:id/seed (200).</summary>
[Serializable]
public class SetClientSeedResponse
{
    public bool ok;
    /// <summary>Preenchido apenas quando ok = false (400).</summary>
    public string error;
}

/// <summary>Dados do seed revelado dentro de RotateResponse.</summary>
[Serializable]
public class RotateRevealedData
{
    /// <summary>Server seed agora exposto — use em POST /verify para auditar spins anteriores.</summary>
    public string serverSeed;
    public string serverSeedHash;
    /// <summary>[nonceInício, nonceFim] dos spins cobertos por este seed.</summary>
    public int[] nonceRange;
}

/// <summary>Resposta de POST /session/:id/rotate (200).</summary>
[Serializable]
public class RotateResponse
{
    public bool ok;
    public RotateRevealedData revealed;
    /// <summary>Hash do novo server seed já ativo para a próxima rodada.</summary>
    public string newServerSeedHash;
}

// ─── Spin ─────────────────────────────────────────────────────────────────────

/// <summary>Body de POST /spin</summary>
[Serializable]
public class SpinRequest
{
    public string sessionId;
    /// <summary>Opcional — null omite o campo do JSON e o backend usa o betPerLine da sessão.</summary>
    public int? betPerLine;
}

/// <summary>Vitória em uma payline individual.</summary>
[Serializable]
public class LineWin
{
    public int lineId;
    public int symbolId;
    /// <summary>Quantos símbolos iguais da esquerda para direita.</summary>
    public int count;
    public int multiplier;
    /// <summary>Moedas ganhas nessa linha (multiplier × betPerLine).</summary>
    public int coins;
    /// <summary>
    /// Posições vencedoras no grid: [[col, row], ...].
    /// Preenchido pelo backend — o cliente usa para animação sem conhecer as paylines.
    /// </summary>
    public int[][] cells;
}

/// <summary>Resultado bruto do spin.</summary>
[Serializable]
public class SpinData
{
    /// <summary>Índice de parada (0-29) de cada um dos 5 reels.</summary>
    public int[] stopPositions;
    /// <summary>
    /// Grade 5 colunas × 3 linhas de symbolIds.
    /// Requer Newtonsoft.Json; com JsonUtility use GridRow[].
    /// </summary>
    public int[][] grid;
    public LineWin[] lineWins;
    public int lineWinTotal;
    public int scatterCount;
    /// <summary>Posições dos Scatters no grid: [[col, row], ...].</summary>
    public int[][] scatterPositions;
    public int scatterCoins;
    public bool triggerFreeSpins;
    public int freeSpinsAwarded;
    public int totalBet;
    public int totalWin;
    /// <summary>Veja WinLevel para os valores possíveis.</summary>
    public string winLevel;
}

/// <summary>Snapshot da sessão retornado junto com o spin.</summary>
[Serializable]
public class SpinSessionData
{
    public int coins;
    public int freeSpinsRemaining;
    public int nonce;
    public string serverSeedHash;
}

/// <summary>Dados para auditoria provably fair do spin.</summary>
[Serializable]
public class ProvablyFairData
{
    public string serverSeedHash;
    public string clientSeed;
    /// <summary>Nonce exato usado neste spin — guarde para auditar via POST /verify.</summary>
    public int nonce;
}

/// <summary>Resposta completa de POST /spin (200).</summary>
[Serializable]
public class SpinResponse : ISpinResult
{
    public SpinData spin;
    public SpinSessionData session;
    public ProvablyFairData provablyFair;
}

// ─── Verify ───────────────────────────────────────────────────────────────────

/// <summary>Body de POST /verify</summary>
[Serializable]
public class VerifyRequest
{
    /// <summary>Server seed revelado via POST /session/:id/rotate.</summary>
    public string serverSeed;
    public string clientSeed;
    public int nonce;
    /// <summary>Stop positions declarados pelo cliente (5 valores 0-29).</summary>
    public int[] stops;
}

/// <summary>Resposta de POST /verify (200).</summary>
[Serializable]
public class VerifyResponse
{
    /// <summary>true se os stops batem com o seed/nonce — prova que o resultado não foi adulterado.</summary>
    public bool valid;
    /// <summary>Stops recalculados pelo servidor para comparação.</summary>
    public int[] recomputedStops;
}

// ─── Constantes ───────────────────────────────────────────────────────────────

/// <summary>Valores possíveis de SpinData.winLevel.</summary>
public static class WinLevel
{
    public const string None = "none";
    public const string Small = "small";
    public const string Big = "big";
    public const string Mega = "mega";
    public const string Jackpot = "jackpot";
}

/// <summary>
/// IDs de símbolo retornados em SpinData.grid e LineWin.symbolId.
/// Correspondem ao índice no PAYTABLE do backend (config.js).
/// </summary>
public static class SymbolId
{
    public const int Husky = 0;
    public const int Golden = 1;
    public const int Shiba = 2;
    public const int Pug = 3;
    public const int Beagle = 4;
    public const int Dachshund = 5;
    public const int Wild = 6;
    public const int Scatter = 7;
    public const int Blank = 8;
}

/// <summary>
/// Alternativa a int[][] compatível com JsonUtility.
/// Substitua SpinData.grid por GridRow[] se não usar Newtonsoft.Json.
/// </summary>
[Serializable]
public class GridRow
{
    public int[] cells;
}

