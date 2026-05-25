using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SessionPresenter
//  Singleton persistente entre cenas. Orquestra o fluxo MVP:
//  chama ApiClient, atualiza SessionModel e comanda a View registrada.
//
//  Ciclo de vida:
//    Awake     → garante instância única (DontDestroyOnLoad)
//    OnDestroy → cancela operações assíncronas em andamento
//
//  Como usar:
//    MenuView  → SessionPresenter.Instance.InitAsync()  (botão Play Game)
//    GameView  → SessionPresenter.Instance.SetView(this) (ao entrar na cena)
//    Botões    → SessionPresenter.Instance.RequestSpinAsync()
// ═══════════════════════════════════════════════════════════════════════════════

public class SessionPresenter : MonoBehaviour, ISpinProvider
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SessionPresenter Instance { get; private set; }

    // ── Estado interno ────────────────────────────────────────────────────────

    private ISessionView _view;
    private ApiClient    _api;
    private SessionModel _model;

    /// <summary>Expõe o model para leitura por outros sistemas (ex.: auditoria, testes).</summary>
    public SessionModel Model => _model;

    // ── CancellationToken ligado ao ciclo de vida do GameObject ───────────────

    private CancellationTokenSource _cts;

    // ═════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida Unity
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _api   = new ApiClient();
        _model = new SessionModel();
        _cts   = new CancellationTokenSource();

        // Eventos do model propagam mudanças à view registrada no momento
        _model.OnCoinsChanged  += coins  => _view?.UpdateCoins(coins);
        _model.OnSpinCompleted += result => _view?.ShowSpinResult(result);
        _model.OnSeedRotated   += result => _view?.ShowRotateResult(result);
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();

        if (Instance == this)
            Instance = null;
    }

    // ── Registro de View ──────────────────────────────────────────────────────

    /// <summary>
    /// Registra a View ativa. Chamado pela SessionView ao entrar na cena do jogo.
    /// Substitui qualquer View anterior (ex.: troca de cena).
    /// </summary>
    public void SetView(ISessionView view)
    {
        _view = view;
        if (_model.HasSession)
            RefreshView();
    }

    /// <summary>Remove o registro da View (chamado por SessionView.OnDestroy).</summary>
    public void ClearView()
    {
        _view = null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Inicialização
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cria uma nova sessão no backend, gera e envia o client seed automaticamente.
    /// Retorna true em caso de sucesso. Chamado pelo MenuView ao clicar em Play Game.
    /// </summary>
    public async UniTask<bool> InitAsync(CancellationToken ct = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        _view?.ShowLoading(true);
        try
        {
            // 1. Cria a sessão
            var sessionResponse = await _api.CreateSessionAsync(linked.Token);
            _model.Apply(sessionResponse);

            // 2. Gera e envia o client seed automaticamente
            string seed = _model.GenerateClientSeed();
            var seedResponse = await _api.SetClientSeedAsync(_model.SessionId, seed, linked.Token);
            if (seedResponse.ok)
                _model.ApplyClientSeed(seed);
            else
                Debug.LogWarning($"[SessionPresenter] Client seed rejeitado pelo backend: {seedResponse.error}");

            RefreshView();
            SlotBridge.BroadcastSession(_model);
            return true;
        }
        catch (ApiException ex)
        {
            HandleError(ex);
            return false;
        }
        finally
        {
            _view?.ShowLoading(false);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Ações públicas — chamadas pelos botões da UI
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Realiza um spin. <paramref name="betPerLine"/> = 0 usa o valor da sessão.
    /// Retorna false se a sessão ainda não foi inicializada.
    /// </summary>
    /// <inheritdoc cref="ISpinProvider.SpinAsync"/>
    public async UniTask<bool> SpinAsync(int betPerLine = 0, CancellationToken ct = default)
    {
        if (!_model.HasSession)
        {
            Debug.LogWarning("[SessionPresenter] Spin solicitado antes de a sessão ser criada.");
            return false;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        _view?.ShowLoading(true);
        try
        {
            var response = await _api.SpinAsync(_model.SessionId, betPerLine, linked.Token);
            _model.ApplySpin(response);
            // OnSpinCompleted e OnCoinsChanged disparam a view automaticamente via eventos
            SlotBridge.BroadcastSession(_model);
            return true;
        }
        catch (ApiException ex)
        {
            HandleError(ex);
            return false;
        }
        finally
        {
            _view?.ShowLoading(false);
        }
    }

    /// <summary>
    /// Define o client seed da sessão (mínimo 8 caracteres, validado pelo backend).
    /// </summary>
    public async UniTask<bool> RequestSetClientSeedAsync(string seed, CancellationToken ct = default)
    {
        if (!_model.HasSession)
        {
            Debug.LogWarning("[SessionPresenter] SetClientSeed solicitado antes de a sessão ser criada.");
            return false;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        _view?.ShowLoading(true);
        try
        {
            var response = await _api.SetClientSeedAsync(_model.SessionId, seed, linked.Token);

            if (response.ok)
            {
                _model.ApplyClientSeed(seed);
                _view?.UpdateClientSeed(seed);
            }
            else
            {
                _view?.ShowError(response.error ?? "Falha ao definir client seed.");
            }

            return response.ok;
        }
        catch (ApiException ex)
        {
            HandleError(ex);
            return false;
        }
        finally
        {
            _view?.ShowLoading(false);
        }
    }

    /// <summary>
    /// Rotaciona o server seed — revela o seed anterior para auditoria provably fair.
    /// </summary>
    public async UniTask<bool> RequestRotateSeedAsync(CancellationToken ct = default)
    {
        if (!_model.HasSession)
        {
            Debug.LogWarning("[SessionPresenter] RotateSeed solicitado antes de a sessão ser criada.");
            return false;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        _view?.ShowLoading(true);
        try
        {
            var response = await _api.RotateSeedAsync(_model.SessionId, linked.Token);
            _model.ApplyRotate(response);
            // OnSeedRotated dispara _view.ShowRotateResult automaticamente via evento
            _view?.UpdateServerSeedHash(_model.ServerSeedHash);
            return true;
        }
        catch (ApiException ex)
        {
            HandleError(ex);
            return false;
        }
        finally
        {
            _view?.ShowLoading(false);
        }
    }

    /// <summary>
    /// Recarrega o estado da sessão atual via GET /session/:id.
    /// Útil para sincronizar após reconexão.
    /// </summary>
    public async UniTask<bool> RequestRefreshSessionAsync(CancellationToken ct = default)
    {
        if (!_model.HasSession)
            return false;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        _view?.ShowLoading(true);
        try
        {
            var response = await _api.GetSessionAsync(_model.SessionId, linked.Token);
            _model.Apply(response);
            RefreshView();
            return true;
        }
        catch (ApiException ex)
        {
            HandleError(ex);
            return false;
        }
        finally
        {
            _view?.ShowLoading(false);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Utilitários privados
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Força a view a exibir todo o estado atual do model de uma vez.
    /// Usado na inicialização e no refresh de sessão.
    /// </summary>
    private void RefreshView()
    {
        if (_view == null) return;
        _view.UpdateCoins(_model.Coins);
        _view.UpdateFreeSpins(_model.FreeSpinsRemaining);
        _view.UpdateServerSeedHash(_model.ServerSeedHash);
        _view.UpdateClientSeed(_model.ClientSeed ?? string.Empty);
    }

    /// <summary>
    /// Trata <see cref="ApiException"/> logando e exibindo mensagem amigável na view.
    /// </summary>
    private void HandleError(ApiException ex)
    {
        Debug.LogError($"[SessionPresenter] Erro de API (HTTP {ex.StatusCode}): {ex.Message}");

        string friendly = ex.StatusCode switch
        {
            400 => "Dados inválidos enviados ao servidor.",
            401 => "Sessão expirada. Tente reiniciar o jogo.",
            404 => "Sessão não encontrada no servidor.",
            429 => "Muitas requisições. Aguarde um momento.",
            >= 500 => "Erro interno do servidor. Tente novamente.",
            0   => "Falha ao processar resposta do servidor.",
            _   => "Ocorreu um erro inesperado.",
        };

        _view?.ShowError(friendly);
    }
}
