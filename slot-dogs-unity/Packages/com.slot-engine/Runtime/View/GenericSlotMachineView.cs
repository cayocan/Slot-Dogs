using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotEngine
{
/// <summary>
/// Implementação genérica de ISlotMachineView.
/// Ponto de partida para qualquer jogo de slot — sem dependências de jogo concreto.
///
/// Para integrar com o resultado do spin do seu jogo, sobrescreva:
///   GetSpinGrid(ISpinResult)  → int[][] (grid col×row) ou null
///   GetTotalWin(ISpinResult)  → int (0 = sem ganho)
/// </summary>
public class GenericSlotMachineView : MonoBehaviour, ISlotMachineView
{
    [Header("Grid de Reels")]
    [SerializeField] private ReelStrip[] _reels;

    [Header("Biblioteca de Símbolos")]
    [SerializeField] private SymbolLibrary _symbolLibrary;

    [Header("Estado da Sessão")]
    [SerializeField] private TMP_Text   _coinsText;
    [SerializeField] private TMP_Text   _freeSpinsText;
    [SerializeField] private GameObject _freeSpinPanel;

    [Header("Info de Vitória")]
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private TMP_Text   _totalWinText;

    [Header("Botões")]
    [SerializeField] private Button     _spinButton;
    [SerializeField] private Button     _autoSpinButton;
    [SerializeField] private GameObject _autoSpinActiveIndicator;
    [SerializeField] private Button     _betIncreaseButton;
    [SerializeField] private Button     _betDecreaseButton;

    [Header("Aposta")]
    [SerializeField] private TMP_Text _betPerLineText;

    [Header("Animação")]
    [SerializeField] private int _reelStopDelayMs = 300;

    // ── Eventos consumidos pelo Presenter ──────────────────────────────────
    public event Action OnSpinRequested;
    public event Action OnAutoSpinToggled;
    public event Action OnBetIncreaseRequested;
    public event Action OnBetDecreaseRequested;

    // ── Ciclo de vida ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (_reels != null && _symbolLibrary != null)
            foreach (var reel in _reels)
                reel?.Init(_symbolLibrary);

        _spinButton?.onClick.AddListener(()        => OnSpinRequested?.Invoke());
        _autoSpinButton?.onClick.AddListener(()    => OnAutoSpinToggled?.Invoke());
        _betIncreaseButton?.onClick.AddListener(() => OnBetIncreaseRequested?.Invoke());
        _betDecreaseButton?.onClick.AddListener(() => OnBetDecreaseRequested?.Invoke());
    }

    private void OnDestroy()
    {
        if (_reels != null)
            foreach (var reel in _reels)
                reel?.KillSpin();

        _spinButton?.onClick.RemoveAllListeners();
        _autoSpinButton?.onClick.RemoveAllListeners();
        _betIncreaseButton?.onClick.RemoveAllListeners();
        _betDecreaseButton?.onClick.RemoveAllListeners();
    }

    // ── ISlotMachineView ───────────────────────────────────────────────────

    public void SetSpinInteractable(bool interactable)
    {
        if (_spinButton != null) _spinButton.interactable = interactable;
    }

    public void UpdateCoins(int coins)
    {
        if (_coinsText != null) _coinsText.text = $"{coins:N0}";
    }

    public void UpdateFreeSpins(int remaining)
    {
        if (_freeSpinsText != null) _freeSpinsText.text = $"{remaining}";
        _freeSpinPanel?.SetActive(remaining > 0);
    }

    public void SetAutoSpinActive(bool active)
    {
        _autoSpinActiveIndicator?.SetActive(active);
    }

    public void StartSpinVisual()
    {
        SetWinPanelVisible(false);
        if (_reels == null) return;
        foreach (var reel in _reels)
            reel?.StartSpin();
    }

    public async UniTask StopSpinVisualAsync(ISpinResult result)
    {
        if (_reels == null) return;

        var grid     = GetSpinGrid(result);
        var totalWin = GetTotalWin(result);

        var tasks = new UniTask[_reels.Length];
        for (int col = 0; col < _reels.Length; col++)
        {
            var symbols = grid != null && col < grid.Length ? grid[col] : null;
            tasks[col]  = StopReelAsync(_reels[col], symbols, col * _reelStopDelayMs);
        }
        await UniTask.WhenAll(tasks);

        ShowWinInfo(totalWin);
    }

    // ── Métodos adicionais (chamados pelo Presenter) ───────────────────────

    public void UpdateBetPerLine(int bet, int minBet, int maxBet, int totalBet)
    {
        if (_betPerLineText != null)    _betPerLineText.text = $"{totalBet}";
        if (_betDecreaseButton != null) _betDecreaseButton.interactable = bet > minBet;
        if (_betIncreaseButton != null) _betIncreaseButton.interactable = bet < maxBet;
    }

    public void Clear()
    {
        if (_reels != null)
            foreach (var reel in _reels)
                reel?.ShowBlank();
        SetWinPanelVisible(false);
    }

    // ── Pontos de extensão para subclasses ────────────────────────────────

    /// <summary>
    /// Sobrescreva para extrair o grid int[col][row] do resultado concreto do jogo.
    /// Retorna null por padrão — reels param na posição de repouso.
    /// </summary>
    protected virtual int[][] GetSpinGrid(ISpinResult result) => null;

    /// <summary>
    /// Sobrescreva para extrair o total ganho do resultado concreto do jogo.
    /// Retorna 0 por padrão.
    /// </summary>
    protected virtual int GetTotalWin(ISpinResult result) => 0;

    // ── Internos ──────────────────────────────────────────────────────────

    private static async UniTask StopReelAsync(ReelStrip reel, int[] symbols, int delayMs)
    {
        if (reel == null) return;
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await reel.StopSpinAsync(symbols);
    }

    private void ShowWinInfo(int totalWin)
    {
        bool hasWin = totalWin > 0;
        SetWinPanelVisible(hasWin);
        if (_totalWinText != null)
            _totalWinText.text = hasWin ? $"+{totalWin:N0}" : string.Empty;
    }

    private void SetWinPanelVisible(bool visible)
    {
        _winPanel?.SetActive(visible);
    }
}
}
