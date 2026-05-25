using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — ApiClient
//  Encapsula todas as chamadas REST ao slot-dogs-backend via UnityWebRequest.
//  Requer:
//    - UniTask         (com.cysharp.unitask)
//    - Newtonsoft.Json (com.unity.nuget.newtonsoft-json)
//
//  A URL base é lida de Assets/Resources/app.env via EnvConfig:
//    Development Build  → DEV_API_URL
//    Release Build      → PROD_API_URL
//
//  Uso:
//    var api = new ApiClient();          // URL resolvida automaticamente
//    var api = new ApiClient(overrideUrl); // URL explícita (testes)
//    var session = await api.CreateSessionAsync();
// ═══════════════════════════════════════════════════════════════════════════════

public class ApiClient
{
    // ── Configuração ─────────────────────────────────────────────────────────

    private readonly string _baseUrl;
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>
    /// Cria o cliente resolvendo a URL a partir de <see cref="EnvConfig.ApiUrl"/>.
    /// Development Build usa DEV_API_URL; Release usa PROD_API_URL (ambos em app.env).
    /// </summary>
    public ApiClient() : this(EnvConfig.ApiUrl) { }

    /// <summary>
    /// Cria o cliente com uma URL explícita (útil para testes).
    /// </summary>
    public ApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /session — Cria uma nova sessão de jogo.
    /// Retorna estado inicial com serverSeedHash e coins.
    /// </summary>
    public async UniTask<SessionStateResponse> CreateSessionAsync(CancellationToken ct = default)
    {
        return await PostAsync<SessionStateResponse>("/session", null, expectedCode: 201, ct: ct);
    }

    /// <summary>
    /// GET /session/:id — Consulta o estado atual da sessão.
    /// </summary>
    public async UniTask<SessionStateResponse> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        ValidateId(sessionId);
        return await GetAsync<SessionStateResponse>($"/session/{sessionId}", ct);
    }

    /// <summary>
    /// POST /session/:id/seed — Define o client seed da sessão (mínimo 8 caracteres).
    /// </summary>
    public async UniTask<SetClientSeedResponse> SetClientSeedAsync(
        string sessionId, string clientSeed, CancellationToken ct = default)
    {
        ValidateId(sessionId);
        var body = new SetClientSeedRequest { clientSeed = clientSeed };
        return await PostAsync<SetClientSeedResponse>($"/session/{sessionId}/seed", body, ct: ct);
    }

    /// <summary>
    /// POST /session/:id/rotate — Rotaciona o server seed, revelando o anterior para auditoria.
    /// </summary>
    public async UniTask<RotateResponse> RotateSeedAsync(string sessionId, CancellationToken ct = default)
    {
        ValidateId(sessionId);
        return await PostAsync<RotateResponse>($"/session/{sessionId}/rotate", null, ct: ct);
    }

    // ── Spin ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /spin — Realiza um spin.
    /// <paramref name="betPerLine"/> é opcional; quando 0 usa o betPerLine da sessão.
    /// </summary>
    public async UniTask<SpinResponse> SpinAsync(
        string sessionId, int betPerLine = 0, CancellationToken ct = default)
    {
        var body = new SpinRequest
        {
            sessionId  = sessionId,
            betPerLine = betPerLine > 0 ? betPerLine : (int?)null,
        };
        return await PostAsync<SpinResponse>("/spin", body, ct: ct);
    }

    // ── Verify ────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /verify — Verifica um spin (provably fair).
    /// Use o serverSeed revelado via RotateSeedAsync para auditar spins anteriores.
    /// </summary>
    public async UniTask<VerifyResponse> VerifySpinAsync(
        string serverSeed, string clientSeed, int nonce, int[] stops, CancellationToken ct = default)
    {
        var body = new VerifyRequest
        {
            serverSeed = serverSeed,
            clientSeed = clientSeed,
            nonce      = nonce,
            stops      = stops,
        };
        return await PostAsync<VerifyResponse>("/verify", body, ct: ct);
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private async UniTask<T> GetAsync<T>(string path, CancellationToken ct)
    {
        string url = _baseUrl + path;
        using var request = UnityWebRequest.Get(url);
        SetCommonHeaders(request);

        await request.SendWebRequest().WithCancellation(ct);

        ThrowIfError(request, url);
        return Deserialize<T>(request.downloadHandler.text);
    }

    private async UniTask<T> PostAsync<T>(
        string path, object body, int expectedCode = 200, CancellationToken ct = default)
    {
        string url      = _baseUrl + path;
        string json     = body != null ? JsonConvert.SerializeObject(body, _jsonSettings) : "{}";
        byte[] rawBody  = Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler   = new UploadHandlerRaw(rawBody),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        SetCommonHeaders(request);
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest().WithCancellation(ct);

        ThrowIfError(request, url);
        return Deserialize<T>(request.downloadHandler.text);
    }

    // ── Utilitários ───────────────────────────────────────────────────────────

    private static void SetCommonHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("Accept", "application/json");
    }

    private static void ThrowIfError(UnityWebRequest request, string url)
    {
        if (request.result == UnityWebRequest.Result.Success) return;

        string body = request.downloadHandler?.text ?? string.Empty;

        // Tenta extrair mensagem de erro estruturada do backend
        string apiError = string.Empty;
        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                var err = JsonConvert.DeserializeObject<ErrorResponse>(body);
                if (err?.error != null) apiError = err.error;
            }
            catch { /* ignora parse errors no caminho de erro */ }
        }

        string message = string.IsNullOrEmpty(apiError)
            ? $"[ApiClient] {request.result} — {url} ({request.responseCode}): {body}"
            : $"[ApiClient] {url} ({request.responseCode}): {apiError}";

        Debug.LogError(message);
        throw new ApiException(message, (int)request.responseCode, apiError);
    }

    private static T Deserialize<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            throw new ApiException($"[ApiClient] Falha ao desserializar resposta: {ex.Message}", 0, json);
        }
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("sessionId não pode ser vazio.", nameof(id));
    }
}

// ─── Exceção tipada ───────────────────────────────────────────────────────────

/// <summary>
/// Lançada quando o backend retorna erro HTTP ou quando a desserialização falha.
/// </summary>
public class ApiException : Exception
{
    /// <summary>HTTP status code (0 quando o erro é de desserialização).</summary>
    public int StatusCode { get; }

    /// <summary>Campo "error" retornado pelo backend, ou o JSON bruto em falhas de parse.</summary>
    public string ApiError { get; }

    public ApiException(string message, int statusCode, string apiError)
        : base(message)
    {
        StatusCode = statusCode;
        ApiError   = apiError;
    }
}
