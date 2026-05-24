using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — MenuView
//  View da tela inicial (menu). Gerencia o botão Play Game e o feedback de loading.
//
//  Setup na cena:
//    1. Adicione este script ao GameObject raiz do menu.
//    2. Conecte os campos via Inspector:
//         _playButton      → botão "Play Game"
//         _loadingPanel    → painel/spinner de loading (desativado por padrão)
//         _errorText       → texto de erro (desativado por padrão)
//    3. Defina _gameSceneName com o nome exato da cena do jogo (ex.: "GameScene").
//    4. O SessionPresenter deve estar em um GameObject na mesma cena, ele
//       persiste automaticamente via DontDestroyOnLoad.
// ═══════════════════════════════════════════════════════════════════════════════

public class MenuView : MonoBehaviour
{
    // ── Referências da UI ─────────────────────────────────────────────────────

    [Header("UI")]
    [SerializeField] private Button       _playButton;
    [SerializeField] private GameObject   _loadingPanel;
    [SerializeField] private TMP_Text     _errorText;

    [Header("Navegação")]
    [Tooltip("Nome exato da cena do jogo registrada em Build Settings.")]
    [SerializeField] private string _gameSceneName = "GameScene";

    // ── CancellationToken ligado à vida da tela ───────────────────────────────

    private CancellationTokenSource _cts;

    // ═════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida Unity
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _cts = new CancellationTokenSource();

        SetError(null);
        SetLoading(false);
    }

    private void Start()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();
        _playButton.onClick.RemoveListener(OnPlayClicked);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Handlers de botão
    // ═════════════════════════════════════════════════════════════════════════

    private void OnPlayClicked()
    {
        HandlePlayAsync(_cts.Token).Forget();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Fluxo principal
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inicia a sessão via SessionPresenter e carrega a cena do jogo em caso de sucesso.
    /// </summary>
    private async UniTaskVoid HandlePlayAsync(CancellationToken ct)
    {
        SetError(null);
        SetLoading(true);
        _playButton.interactable = false;

        bool ok = await SessionPresenter.Instance.InitAsync(ct);

        if (ct.IsCancellationRequested) return;

        if (ok)
        {
            // Sessão criada com sucesso → vai para o jogo
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            // Erro já logado e exibido pelo Presenter via _view (sem view aqui,
            // então exibimos o erro direto nesta tela)
            SetError("Não foi possível conectar ao servidor.\nVerifique sua conexão e tente novamente.");
            SetLoading(false);
            _playButton.interactable = true;
        }
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
}
