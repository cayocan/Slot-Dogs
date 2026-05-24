using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — SlotMachineView (estático, sem animação)
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
    // ── Grid ──────────────────────────────────────────────────────────────────

    [Header("Grid de Símbolos  (5 colunas × 3 linhas = 15 cells)")]
    [Tooltip("Ordem: col0-row0, col0-row1, col0-row2, col1-row0 … col4-row2")]
    [SerializeField] private TMP_Text[] _cells; // deve ter exatamente 15 elementos

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
        Clear();
        _spinButton?.onClick.AddListener(() => OnSpinRequested?.Invoke());
        _betIncreaseButton?.onClick.AddListener(() => OnBetIncreaseRequested?.Invoke());
        _betDecreaseButton?.onClick.AddListener(() => OnBetDecreaseRequested?.Invoke());
    }

    private void OnDestroy()
    {
        _spinButton?.onClick.RemoveAllListeners();
        _betIncreaseButton?.onClick.RemoveAllListeners();
        _betDecreaseButton?.onClick.RemoveAllListeners();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API pública — chamada pelo SlotMachinePresenter
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Popula o grid com os símbolos do spin e exibe o painel de vitória se houver ganho.
    /// </summary>
    public void Populate(SpinResponse result)
    {
        if (result?.spin == null) return;

        PopulateGrid(result.spin.grid);
        ShowWinInfo(result.spin.totalWin, result.spin.winLevel);
    }

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

    /// <summary>Reseta todos os cells para "—" e oculta o painel de vitória.</summary>
    public void Clear()
    {
        if (_cells != null)
            foreach (var cell in _cells)
                if (cell != null) cell.text = "—";

        SetWinPanelVisible(false);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Internos
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Escreve os nomes dos símbolos nos TMP_Text do grid.
    /// grid[col][row]: col = índice do reel (0-4), row = linha (0-2, topo→base).
    /// </summary>
    private void PopulateGrid(int[][] grid)
    {
        if (_cells == null || grid == null) return;

        for (int col = 0; col < 5 && col < grid.Length; col++)
        {
            for (int row = 0; row < 3 && row < grid[col].Length; row++)
            {
                int idx = col * 3 + row;
                if (idx >= _cells.Length || _cells[idx] == null) continue;

                _cells[idx].text = SymbolLabel(grid[col][row]);
            }
        }
    }

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

    // ── Mapeamento symbolId → label ───────────────────────────────────────────

    private static string SymbolLabel(int id) => id switch
    {
        SymbolId.Husky     => "HUSKY",
        SymbolId.Golden    => "GOLDEN",
        SymbolId.Shiba     => "SHIBA",
        SymbolId.Pug       => "PUG",
        SymbolId.Beagle    => "BEAGLE",
        SymbolId.Dachshund => "DACHSH",
        SymbolId.Wild      => "★WILD★",
        SymbolId.Scatter   => "◆SCAT◆",
        SymbolId.Blank     => "—",
        _                  => $"?{id}",
    };
}
