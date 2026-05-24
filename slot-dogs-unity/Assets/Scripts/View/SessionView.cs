using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SessionView
//  View da cena do jogo. Implementa ISessionView e se registra no SessionPresenter.
//
//  Setup na cena:
//    1. Adicione este script ao GameObject raiz da UI do jogo.
//    2. Conecte os campos via Inspector.
//    3. Os botões chamam os métodos públicos Request* do SessionPresenter.Instance.
// ═══════════════════════════════════════════════════════════════════════════════

public class SessionView : MonoBehaviour, ISessionView
{
    // ── Displays de estado ────────────────────────────────────────────────────

    [Header("Estado da Sessão")]
    [SerializeField] private TMP_Text _coinsText;
    [SerializeField] private TMP_Text _freeSpinsText;
    [SerializeField] private TMP_Text _serverSeedHashText;
    [SerializeField] private TMP_Text _clientSeedText;

    // ── Feedback de loading / erro ────────────────────────────────────────────

    [Header("Feedback")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TMP_Text _errorText;

    // ── Botões ────────────────────────────────────────────────────────────────

    [Header("Botões")]

    [SerializeField] private Button _rotateSeedButton;

    // ═════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida Unity
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        SetError(null);
        SetLoading(false);
    }

    private void Start()
    {
        // Registra esta view no Presenter persistente
        SessionPresenter.Instance.SetView(this);

        // Conecta botões
        _rotateSeedButton?.onClick.AddListener(OnRotateSeedClicked);
    }

    private void OnDestroy()
    {
        // Remove registro para evitar referência nula em cenas futuras
        SessionPresenter.Instance?.ClearView();

        _rotateSeedButton?.onClick.RemoveListener(OnRotateSeedClicked);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Handlers de botão
    // ═════════════════════════════════════════════════════════════════════════

    private void OnRotateSeedClicked()
    {
        RotateSeedAsync().Forget();
    }

    private async UniTaskVoid RotateSeedAsync()
    {
        await SessionPresenter.Instance.RequestRotateSeedAsync();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ISessionView — chamados pelo Presenter
    // ═════════════════════════════════════════════════════════════════════════

    public void ShowLoading(bool isLoading)
    {
        SetLoading(isLoading);

        // Desabilita o botão de auditoria durante operações assíncronas
        if (_rotateSeedButton != null) _rotateSeedButton.interactable = !isLoading;
    }

    public void UpdateCoins(int coins)
    {
        if (_coinsText != null)
            _coinsText.text = coins.ToString("N0");
    }

    public void UpdateFreeSpins(int freeSpinsRemaining)
    {
        if (_freeSpinsText != null)
        {
            _freeSpinsText.gameObject.SetActive(freeSpinsRemaining > 0);
            _freeSpinsText.text = $"Free Spins: {freeSpinsRemaining}";
        }
    }

    public void UpdateServerSeedHash(string hash)
    {
        if (_serverSeedHashText != null)
            _serverSeedHashText.text = TruncateHash(hash);
    }

    public void UpdateClientSeed(string seed)
    {
        if (_clientSeedText != null)
            _clientSeedText.text = TruncateHash(seed);
    }

    public void ShowSpinResult(SpinResponse result)
    {
        SetError(null);

        if (result?.spin == null) return;

        // Exibe feedback de vitória se houver ganho
        if (result.spin.totalWin > 0)
            ShowWinFeedback(result.spin.totalWin, result.spin.winLevel);
    }

    public void ShowRotateResult(RotateResponse result)
    {
        if (result?.revealed == null) return;

        // Exibe o server seed revelado para auditoria
        string msg = $"Seed revelado: {result.revealed.serverSeed}\n" +
            $"Nonces: {result.revealed.nonceRange[0]}–{result.revealed.nonceRange[1]}";
        Debug.Log($"[SessionView] {msg}");
        // TODO: exibir em painel de auditoria dedicado
    }

    public void ShowError(string message)
    {
        SetError(message);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Utilitários de UI
    // ═════════════════════════════════════════════════════════════════════════

    private void SetLoading(bool isLoading)
    {
        if (_loadingPanel != null)
            _loadingPanel.SetActive(isLoading);
    }

    private void SetError(string message)
    {
        if (_errorText == null) return;
        bool hasError = !string.IsNullOrEmpty(message);
        _errorText.gameObject.SetActive(hasError);
        if (hasError) _errorText.text = message;
    }

    private void ShowWinFeedback(int totalWin, string winLevel)
    {
        // TODO — substitua por animações reais conforme o winLevel
        Debug.Log($"[SessionView] Vitória! +{totalWin} coins | level: {winLevel}");
    }

    /// <summary>Exibe apenas os primeiros e últimos 6 chars de um hash longo.</summary>
    private static string TruncateHash(string hash)
    {
        if (string.IsNullOrEmpty(hash) || hash.Length <= 16) return hash ?? string.Empty;
        return $"{hash[..6]}...{hash[^6..]}";
    }
}
