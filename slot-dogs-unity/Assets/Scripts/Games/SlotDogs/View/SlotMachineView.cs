using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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

public class SlotMachineView : MonoBehaviour, ISlotMachineView
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

    [Header("Contador de Ganho (Big+)")]
    [Tooltip("Texto que exibe o total acumulado enquanto as linhas s\u00e3o animadas. S\u00f3 aparece em wins BIG, MEGA e JACKPOT.")]
    [SerializeField] private TMP_Text _runningWinText;

    // ── Estado da sessão ──────────────────────────────────────────────────────

    [Header("Estado da Sessão")]
    [SerializeField] private TMP_Text   _coinsText;
    [SerializeField] private TMP_Text   _freeSpinsText;
    [Tooltip("Ícone/painel de free spin — ativa/desativa junto com o texto")]
    [SerializeField] private GameObject _freeSpinIcon;

    // ── Botão Spin ────────────────────────────────────────────────────────────

    [Header("Botões")]
    [SerializeField] private Button     _spinButton;
    [SerializeField] private Button     _autoSpinButton;
    [Tooltip("GameObject ativado enquanto o auto-spin estiver ligado")]
    [SerializeField] private GameObject _autoSpinActiveIndicator;
    [SerializeField] private Button     _betIncreaseButton;
    [SerializeField] private Button     _betDecreaseButton;

    // ── Controle de aposta ──────────────────────────────────────────────────

    [Header("Aposta por Linha")]
    [SerializeField] private TMP_Text _betPerLineText;

    // ── Delay escalonado entre reels ──────────────────────────────────────────

    [Header("Animação de Giro")]
    [Tooltip("Delay em ms entre a parada de cada reel (esq→dir)")]
    [SerializeField] private int _reelStopDelayMs = 300;

    [Header("Efeitos Visuais")]
    [Tooltip("Particle system ativado em wins BIG, MEGA e JACKPOT")]
    [SerializeField] private ParticleSystem _multiplierParticles;

    [Header("Contador de Free Spins")]
    [Tooltip("Painel que exibe a contagem de free spins ganhos (1 → X)")]
    [SerializeField] private GameObject _freeSpinsCounterPanel;
    [SerializeField] private TMP_Text   _freeSpinsCounterText;

    [Header("Debug")]
    [Tooltip("Quando ativo, imprime o grid do resultado no Console após cada giro")]
    [SerializeField] private bool _debugLogGrid;

    // ── Eventos (consumidos pelo SlotMachinePresenter) ────────────────────────

    /// <summary>Disparado quando o jogador clica no botão Spin.</summary>
    public event Action OnSpinRequested;

    /// <summary>Disparado quando o jogador ativa ou desativa o auto-spin.</summary>
    public event Action OnAutoSpinToggled;

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
        _autoSpinButton?.onClick.AddListener(() => OnAutoSpinToggled?.Invoke());
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

    // ═════════════════════════════════════════════════════════════════════════
    //  API pública — chamada pelo SlotMachinePresenter
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Atualiza o display de moedas.</summary>
    public void UpdateCoins(int coins)
    {
        if (_coinsText != null)
            _coinsText.text = $"{coins:N0}";
    }

    /// <summary>Atualiza o display de aposta por linha e os botões +/−.</summary>
    public void UpdateBetPerLine(int bet, int minBet, int maxBet, int totalBet)
    {
        if (_betPerLineText != null)
            _betPerLineText.text = $"{totalBet}";

        if (_betDecreaseButton != null)
            _betDecreaseButton.interactable = bet > minBet;

        if (_betIncreaseButton != null)
            _betIncreaseButton.interactable = bet < maxBet;
    }

    /// <summary>Atualiza o display de free spins (oculta quando zero).</summary>
    public void UpdateFreeSpins(int remaining)
    {
        bool active = remaining > 0;
        if (_freeSpinsText != null)
        {
            _freeSpinsText.gameObject.SetActive(active);
            if (active) _freeSpinsText.text = $"{remaining}";
        }
        _freeSpinIcon?.SetActive(active);
    }

    /// <summary>Habilita ou desabilita o botão Spin.</summary>
    public void SetSpinInteractable(bool interactable)
    {
        if (_spinButton != null)
            _spinButton.interactable = interactable;
    }

    /// <summary>Ativa ou desativa o indicador visual de auto-spin.</summary>
    public void SetAutoSpinActive(bool active)
    {
        _autoSpinActiveIndicator?.SetActive(active);
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

        // Esconde o contador acumulado quando o painel de ganho aparece
        if (visible && _runningWinText != null)
            _runningWinText.gameObject.SetActive(false);
    }

    // ── Animação ──────────────────────────────────────────────────────────────

    /// <summary>Inicia o giro em todos os reels simultaneamente.</summary>
    public void StartSpinVisual()
    {
        SetWinPanelVisible(false);

        if (_runningWinText != null)
            _runningWinText.gameObject.SetActive(false);

        if (_freeSpinsCounterPanel != null)
            _freeSpinsCounterPanel.SetActive(false);

        if (_multiplierParticles != null)
            _multiplierParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (_reels == null) return;
        foreach (var reel in _reels)
            reel?.StartSpin();
    }

    /// <summary>
    /// Para os reels esquerda→direita com delay escalonado e exibe o resultado.
    /// </summary>
    public async UniTask StopSpinVisualAsync(ISpinResult result)
    {
        // Cast seguro: SlotMachineView é Slot Dogs-específico e sempre recebe SpinResponse.
        var response = (SpinResponse)result;
        if (response?.spin?.grid == null || _reels == null) return;

        var tasks = new UniTask[_reels.Length];
        for (int col = 0; col < _reels.Length; col++)
        {
            var reel    = _reels[col];
            var symbols = col < response.spin.grid.Length ? response.spin.grid[col] : new int[3];
            tasks[col]  = StopReelAsync(reel, symbols, col * _reelStopDelayMs);
        }

        await UniTask.WhenAll(tasks);

        if (_debugLogGrid) LogGrid(response.spin.grid);

        // Contador de free spins ganhos (exibido antes dos winners)
        if (response.spin.freeSpinsAwarded > 0)
            await AnimateFreeSpinCounterAsync(response.spin.freeSpinsAwarded);

        // Anima os símbolos vencedores antes de exibir o painel de ganhos
        await AnimateWinnersAsync(response);

        // Pausa extra de 0.75 s após as animações de resultado
        if (response.spin.totalWin > 0)
            await UniTask.Delay(750);

        ShowWinInfo(response.spin.totalWin, response.spin.winLevel);
    }

    private static async UniTask StopReelAsync(ReelStrip reel, int[] symbols, int delayMs)
    {
        if (reel == null) return;
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await reel.StopSpinAsync(symbols);
    }

    // ── Animação de vencedores ────────────────────────────────────────────────

    /// <summary>
    /// Exibe cada ganho separadamente, um após o outro.
    /// Em wins BIG, MEGA e JACKPOT exibe também um contador acumulado
    /// que cresce em tamanho e faz pulse a cada linha.
    /// </summary>
    private async UniTask AnimateWinnersAsync(SpinResponse result)
    {
        if (result?.spin == null || result.spin.totalWin <= 0) return;

        // ── Monta grupos ordenados: (células, moedas) ─────────────────────────

        var winGroups = new List<(List<(int col, int row)> cells, int coins)>();

        if (result.spin.lineWins != null)
            foreach (var win in result.spin.lineWins)
            {
                var group = new List<(int col, int row)>();
                if (win.cells != null)
                    foreach (var cell in win.cells)
                    { if (cell.Length >= 2) group.Add((cell[0], cell[1])); }
                else
                    // Fallback sem 'cells': anima apenas a fileira central de cada coluna
                    for (int col = 0; col < win.count; col++)
                        group.Add((col, 1));
                if (group.Count > 0)
                    winGroups.Add((group, win.coins));
            }

        if (result.spin.scatterPositions != null && result.spin.scatterPositions.Length > 0)
        {
            var sc = new List<(int col, int row)>();
            foreach (var pos in result.spin.scatterPositions)
                if (pos.Length >= 2) sc.Add((pos[0], pos[1]));
            if (sc.Count > 0)
                winGroups.Add((sc, result.spin.scatterCoins));
        }

        if (winGroups.Count == 0)
        {
            Debug.LogWarning("[SlotMachineView] AnimateWinnersAsync: sem células identificadas " +
                             "(reinicie o backend para ativar 'cells' e 'scatterPositions').");
            return;
        }

        // ── Particle system: ativo em wins BIG, MEGA e JACKPOT ───────────────────

        if (_multiplierParticles != null &&
            result.spin.winLevel is "big" or "mega" or "jackpot")
        {
            _multiplierParticles.Play();
        }

        // ── Contador acumulado: ativo apenas em BIG, MEGA, JACKPOT ───────────

        bool showCounter = result.spin.winLevel is "big" or "mega" or "jackpot"
                        && _runningWinText != null;

        int   accumulated = 0;
        float textScale   = 1f;

        if (showCounter)
        {
            _runningWinText.transform.localScale = Vector3.one;
            _runningWinText.text = string.Empty;
            _runningWinText.gameObject.SetActive(true);
        }

        // ── Anima cada grupo em sequência ─────────────────────────────────────

        for (int i = 0; i < winGroups.Count; i++)
        {
            var (cells, coins) = winGroups[i];
            await AnimateCellGroupAsync(cells);

            if (!showCounter) continue;

            accumulated += coins;
            textScale    = Mathf.Min(textScale + 0.2f, 2f);
            _runningWinText.text = $"+{accumulated:N0}";

            bool isLast = i == winGroups.Count - 1;
            var  pulseTcs = new UniTaskCompletionSource();

            _runningWinText.transform.DOKill();
            float pulsePeak = Mathf.Min(textScale * 1.4f, 2f);
            DOTween.Sequence()
                .Append(_runningWinText.transform
                    .DOScale(Vector3.one * pulsePeak, 0.08f).SetEase(Ease.OutQuad))
                .Append(_runningWinText.transform
                    .DOScale(Vector3.one * textScale, 0.15f).SetEase(Ease.OutBounce))
                .OnComplete(() => pulseTcs.TrySetResult())
                .OnKill   (() => pulseTcs.TrySetResult());

            // Aguarda o pulse apenas do último ganho; os demais sobrepõem o próximo grupo
            if (isLast)
            {
                await pulseTcs.Task;
                await UniTask.Delay(300); // pausa final antes do painel de ganho
            }
        }
    }

    /// <summary>
    /// Anima um grupo de células em paralelo (zoom + giro) e aguarda a conclusão.
    /// Cada linha de ganho é um grupo independente chamado sequencialmente.
    /// </summary>
    private async UniTask AnimateCellGroupAsync(List<(int col, int row)> group)
    {
        var pending = new List<UniTaskCompletionSource>();

        foreach (var (col, row) in group)
        {
            if (col < 0 || col >= _reels.Length) continue;
            var instance = _reels[col]?.GetResultInstance(row);
            if (instance == null) continue;

            // Funciona com Transform (sprites) e RectTransform (canvas UI)
            var tr  = instance.transform;
            var tcs = new UniTaskCompletionSource();
            pending.Add(tcs);

            // zoom in → gira esq → gira dir → volta ao normal (~0.52 s por grupo)
            DOTween.Sequence()
                .Append(tr.DOScale(Vector3.one * 1.15f, 0.12f).SetEase(Ease.OutQuad))
                .Append(tr.DOLocalRotate(new Vector3(0f, 0f, -8f), 0.10f).SetEase(Ease.OutQuad))
                .Append(tr.DOLocalRotate(new Vector3(0f, 0f,  8f), 0.20f).SetEase(Ease.InOutQuad))
                .Append(tr.DOLocalRotate(Vector3.zero,             0.10f).SetEase(Ease.InQuad))
                .Join  (tr.DOScale(Vector3.one,                    0.10f).SetEase(Ease.InQuad))
                .OnComplete(() => tcs.TrySetResult())
                .OnKill   (() => tcs.TrySetResult());
        }

        if (pending.Count > 0)
        {
            var waitAll = new UniTask[pending.Count];
            for (int i = 0; i < pending.Count; i++) waitAll[i] = pending[i].Task;
            await UniTask.WhenAll(waitAll);
        }
    }

    // ── Contador de Free Spins ────────────────────────────────────────────────

    /// <summary>
    /// Exibe o painel contador de free spins e conta de 1 até
    /// <paramref name="totalFreeSpins"/> com a mesma animação de pulse
    /// do contador acumulado (BIG/MEGA/JACKPOT).
    /// </summary>
    private async UniTask AnimateFreeSpinCounterAsync(int totalFreeSpins)
    {
        if (_freeSpinsCounterPanel == null || _freeSpinsCounterText == null) return;

        _freeSpinsCounterPanel.SetActive(true);
        _freeSpinsCounterText.transform.localScale = Vector3.one;

        for (int i = 1; i <= totalFreeSpins; i++)
        {
            _freeSpinsCounterText.text = i.ToString();

            var tcs = new UniTaskCompletionSource();
            _freeSpinsCounterText.transform.DOKill();
            DOTween.Sequence()
                .Append(_freeSpinsCounterText.transform
                    .DOScale(Vector3.one * 1.4f, 0.08f).SetEase(Ease.OutQuad))
                .Append(_freeSpinsCounterText.transform
                    .DOScale(Vector3.one,        0.15f).SetEase(Ease.OutBounce))
                .OnComplete(() => tcs.TrySetResult())
                .OnKill   (() => tcs.TrySetResult());

            await tcs.Task;

            // Pausa entre cada número (exceto após o último)
            if (i < totalFreeSpins)
                await UniTask.Delay(150);
        }

        // Pausa final para o jogador ler o total
        await UniTask.Delay(600);
        _freeSpinsCounterPanel.SetActive(false);
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    private static void LogGrid(int[][] grid)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[SlotMachineView] Grid do spin (col→ / row↓):");
        int rows = grid[0].Length;
        for (int row = 0; row < rows; row++)
        {
            sb.Append($"  row {row}:");
            for (int col = 0; col < grid.Length; col++)
                sb.Append($"  {grid[col][row],2}");
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
}
