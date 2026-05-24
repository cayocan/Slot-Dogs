using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SlotMachineView
//  Responsabilidades exclusivas de UI:
//    • Exibir o grid 5×3 de símbolos
//    • Exibir totalWin, winLevel, coins, freeSpins
//    • Controlar interatividade do botão Spin
//    • Disparar evento OnSpinRequested quando o jogador clica em Spin
//  Quem chama os métodos públicos: SlotMachinePresenter.
//
//  Setup na cena (GameScene):
//    1. Adicione este script ao Panel "SlotMachine" do Canvas.
//    2. Conecte todos os campos via Inspector (ver regiões [Header]).
//    3. Arraste este componente para o campo _view do SlotMachinePresenter.
// ═══════════════════════════════════════════════════════════════════════════════

public class SlotMachineView : MonoBehaviour
{
    // ── Grid de Reels ──────────────────────────────────────────────────────────

    [Header("Grid  (5 Reels)")]
    [Tooltip("Um ReelStrip por coluna, da esquerda para a direita")]
    [SerializeField] private ReelStrip[] _reels;   // deve ter exatamente 5 elementos

    [Header("Biblioteca de Símbolos")]
    [Tooltip("Asset SymbolLibrary com o mapeamento symbolId → prefab")]
    [SerializeField] private SymbolLibrary _symbolLibrary;

    // ── Win info ──────────────────────────────────────────────────────────────

    [Header("Info de Vitória")]
    [SerializeField] private GameObject _winInfoPanel;
    [SerializeField] private TMP_Text   _totalWinText;
    [SerializeField] private TMP_Text   _winLevelText;

    // ── Estado da sessão ──────────────────────────────────────────────────────

    [Header("Estado da Sessão")]
    [SerializeField] private TMP_Text _coinsText;
    [SerializeField] private TMP_Text _freeSpinsText;

    // ── Botão Spin ────────────────────────────────────────────────────────────

    [Header("Botões")]
    [SerializeField] private Button _spinButton;
    [SerializeField] private Button _betIncreaseButton;
    [SerializeField] private Button _betDecreaseButton;

    // ── Controle de aposta ──────────────────────────────────────────────────

    [Header("Aposta por Linha")]
    [SerializeField] private TMP_Text _betPerLineText;

    // ── Delay escalonado entre reels ──────────────────────────────────────────

    [Header("Animação de Giro")]
    [Tooltip("Delay em ms entre a parada de cada reel (esq→dir)")]
    [SerializeField] private int _reelStopDelayMs = 300;

    // ── Eventos (consumidos pelo SlotMachinePresenter) ────────────────────────

    /// <summary>Disparado quando o jogador clica no botão Spin.</summary>
    public event Action OnSpinRequested;

    /// <summary>Disparado quando o jogador clica em "+" no bet.</summary>
    public event Action OnBetIncreaseRequested;

    /// <summary>Disparado quando o jogador clica em "-" no bet.</summary>
    public event Action OnBetDecreaseRequested;

    // ═════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida Unity
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Injeta a SymbolLibrary em cada reel antes de qualquer Init
        if (_reels != null && _symbolLibrary != null)
            foreach (var reel in _reels)
                reel?.Init(_symbolLibrary);

        _spinButton?.onClick.AddListener(() => OnSpinRequested?.Invoke());
        _betIncreaseButton?.onClick.AddListener(() => OnBetIncreaseRequested?.Invoke());
        _betDecreaseButton?.onClick.AddListener(() => OnBetDecreaseRequested?.Invoke());
    }

    private void OnDestroy()
    {
        if (_reels != null)
            foreach (var reel in _reels)
                reel?.KillSpin();

        _spinButton?.onClick.RemoveAllListeners();
        _betIncreaseButton?.onClick.RemoveAllListeners();
        _betDecreaseButton?.onClick.RemoveAllListeners();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API pública — chamada pelo SlotMachinePresenter
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Atualiza o display de moedas.</summary>
    public void UpdateCoins(int coins)
    {
        if (_coinsText != null)
            _coinsText.text = $"Coins: {coins:N0}";
    }

    /// <summary>Atualiza o display de aposta por linha e os botões +/−.</summary>
    public void UpdateBetPerLine(int bet, int minBet, int maxBet)
    {
        if (_betPerLineText != null)
            _betPerLineText.text = $"Bet: {bet}";

        if (_betDecreaseButton != null)
            _betDecreaseButton.interactable = bet > minBet;

        if (_betIncreaseButton != null)
            _betIncreaseButton.interactable = bet < maxBet;
    }

    /// <summary>Atualiza o display de free spins (oculta quando zero).</summary>
    public void UpdateFreeSpins(int remaining)
    {
        if (_freeSpinsText == null) return;
        bool active = remaining > 0;
        _freeSpinsText.gameObject.SetActive(active);
        if (active) _freeSpinsText.text = $"Free Spins: {remaining}";
    }

    /// <summary>Habilita ou desabilita o botão Spin.</summary>
    public void SetSpinInteractable(bool interactable)
    {
        if (_spinButton != null)
            _spinButton.interactable = interactable;
    }

    /// <summary>Limpa o painel de vitória e exibe Blank em todos os reels.</summary>
    public void Clear()
    {
        if (_reels != null)
            foreach (var reel in _reels)
                reel?.ShowBlank();

        SetWinPanelVisible(false);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Internos
    // ═════════════════════════════════════════════════════════════════════════

    private void ShowWinInfo(int totalWin, string winLevel)
    {
        bool hasWin = totalWin > 0;
        SetWinPanelVisible(hasWin);

        if (_totalWinText != null)
            _totalWinText.text = hasWin ? $"+{totalWin:N0}" : string.Empty;

        if (_winLevelText != null)
            _winLevelText.text = hasWin ? (winLevel?.ToUpperInvariant() ?? string.Empty) : string.Empty;
    }

    private void SetWinPanelVisible(bool visible)
    {
        if (_winInfoPanel != null)
            _winInfoPanel.SetActive(visible);
    }

    // ── Animação ──────────────────────────────────────────────────────────────

    /// <summary>Inicia o giro em todos os reels simultaneamente.</summary>
    public void StartSpinVisual()
    {
        SetWinPanelVisible(false);
        if (_reels == null) return;
        foreach (var reel in _reels)
            reel?.StartSpin();
    }

    /// <summary>
    /// Para os reels esquerda→direita com delay escalonado e exibe o resultado.
    /// </summary>
    public async UniTask StopSpinVisualAsync(SpinResponse result)
    {
        if (result?.spin?.grid == null || _reels == null) return;

        var tasks = new UniTask[_reels.Length];
        for (int col = 0; col < _reels.Length; col++)
        {
            var reel    = _reels[col];
            var symbols = col < result.spin.grid.Length ? result.spin.grid[col] : new int[3];
            tasks[col]  = StopReelAsync(reel, symbols, col * _reelStopDelayMs);
        }

        await UniTask.WhenAll(tasks);
        ShowWinInfo(result.spin.totalWin, result.spin.winLevel);
    }

    private static async UniTask StopReelAsync(ReelStrip reel, int[] symbols, int delayMs)
    {
        if (reel == null) return;
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await reel.StopSpinAsync(symbols);
    }
}
