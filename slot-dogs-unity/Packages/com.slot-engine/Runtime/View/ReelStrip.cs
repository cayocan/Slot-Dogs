using DG.Tweening;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SlotEngine
{
// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Engine — ReelStrip
//  Controla a animação de rolagem de um único reel (coluna) da slot machine.
//
//  ── Hierarquia esperada na cena ───────────────────────────────────────────────
//  [Reel_N]  ← este componente + RectTransform (height=3×cellH) + Image + Mask
//    └── [Strip]   ← RectTransform pivot=TOP, height = 6 × cellHeight
//         ├── [Slot_0]  (RectTransform container)  ─┐ resultado: visíveis quando strip.y = 0
//         ├── [Slot_1]  (RectTransform container)   │   (arraste para _slotContainers[0..2])
//         ├── [Slot_2]  (RectTransform container)  ─┘
//         ├── [Slot_3]  (RectTransform container)  ─┐ extra spin: visíveis quando strip.y = 3×cellH
//         ├── [Slot_4]  (RectTransform container)   │   (aleatorizados a cada ciclo de spin)
//         └── [Slot_5]  (RectTransform container)  ─┘
//
//  ── Como funciona ─────────────────────────────────────────────────────────────
//  • strip.anchoredPosition.y = 0    → slots 0,1,2 visíveis (repouso)
//  • strip.anchoredPosition.y = 3H   → slots 3,4,5 visíveis (início do ciclo)
//  • DOAnchorPosY(0, t).LoopType.Restart → símbolos "caem" do topo repetidamente
//  • Ao parar: easing suave até y=0 + micro-bounce (overshoot de 5 px, retorno)
//
//  ── Configuração de Layout no Inspector ───────────────────────────────────────
//  Strip RectTransform:
//    • Pivot:  (0.5, 1)  — topo central
//    • Anchor: (0.5, 1)  — ancora no topo do Reel
//    • Width:  igual ao Reel
//    • Height: 6 × cellHeight  (ex.: 6 × 150 = 900 px)
//    • anchoredPosition: (0, 0)
//  Cada Slot:
//    • Pivot & Anchor: (0.5, 1) — topo central
//    • anchoredPosition: Slot_N = (0, −N × cellHeight)
//    • Height: cellHeight  (ex.: 150 px)
// ═══════════════════════════════════════════════════════════════════════════════

[RequireComponent(typeof(RectTransform))]
public class ReelStrip : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Referências da Strip")]
    [Tooltip("RectTransform da strip interna (pivot TOP, height = 6 × cellHeight)")]
    [SerializeField] private RectTransform _strip;

    [Tooltip("6 containers de cima para baixo: [0,1,2] = resultado | [3,4,5] = extra spin")]
    [SerializeField] private RectTransform[] _slotContainers;  // exatamente 6 elementos

    [Header("Parâmetros de Animação")]
    [Tooltip("Altura de cada célula em pixels — deve coincidir com o layout")]
    [SerializeField] private float _cellHeight        = 150f;

    [Tooltip("Duração de um ciclo de spin (segundos)")]
    [SerializeField] private float _spinCycleDuration = 0.35f;

    [Tooltip("Duração da desaceleração ao parar (segundos)")]
    [SerializeField] private float _stopDuration      = 0.55f;

    [Tooltip("Easing da desaceleração")]
    [SerializeField] private Ease  _stopEase          = Ease.OutQuart;

    [Tooltip("Overshoot do micro-bounce ao parar (pixels)")]
    [SerializeField] private float _bounceOvershoot   = 5f;

    // ── Estado interno ────────────────────────────────────────────────────────

    private SymbolLibrary _library;
    private GameObject[]   _instances;  // instâncias atuais, uma por container

    private float _restY;      // anchoredPosition.y de repouso = 0
    private float _spinFromY;  // anchoredPosition.y início de ciclo = 3 × cellHeight

    private Tweener                 _spinTween;
    private UniTaskCompletionSource _stopTcs;

    // ═════════════════════════════════════════════════════════════════════════
    //  Unity
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _restY     = 0f;
        _spinFromY = 3f * _cellHeight;
    }

    private void OnDestroy()
    {
        KillSpin();
        DestroyAllInstances();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API pública — chamada por SlotMachineView
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Injeta a SymbolLibrary e inicializa com Blank.
    /// Chamado por SlotMachineView.Awake().
    /// </summary>
    public void Init(SymbolLibrary library)
    {
        _library   = library;
        _instances = new GameObject[_slotContainers?.Length ?? 6];
        ShowRandom();
    }

    /// <summary>Inicia a animação de giro contínuo.</summary>
    public void StartSpin()
    {
        _spinTween?.Kill();

        // Randomiza os extra slots e posiciona a strip no início do ciclo
        RandomizeRange(3, 3);
        _strip.anchoredPosition = new Vector2(0f, _spinFromY);

        // Loop: rola de _spinFromY → _restY; a cada ciclo randomiza os extras fora da tela
        _spinTween = _strip.DOAnchorPosY(_restY, _spinCycleDuration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnStepComplete(() =>
            {
                // slots 3,4,5 estão fora da tela quando y = 0 → randomização invisível
                RandomizeRange(3, 3);
                // DOTween (LoopType.Restart) repõe automaticamente para _spinFromY
            });
    }

    /// <summary>
    /// Para o giro e anima até o resultado final com ease-out + micro-bounce.
    /// Aguarde com <c>await</c> para saber quando o reel está totalmente parado.
    /// </summary>
    public UniTask StopSpinAsync(int[] finalSymbols)
    {
        if (_strip == null) return UniTask.CompletedTask;

        // Escreve o resultado nos slots 0,1,2 (estão fora da tela enquanto strip.y > 0)
        SetSlots(0, finalSymbols);

        // Cancela o loop de spin; a strip fica no ponto intermediário atual
        _spinTween?.Kill();
        _spinTween = null;

        _stopTcs = new UniTaskCompletionSource();

        // Decelera até a posição de repouso
        _spinTween = _strip.DOAnchorPosY(_restY, _stopDuration)
            .SetEase(_stopEase)
            .OnComplete(() =>
            {
                _spinTween = null;

                // Micro-bounce: 5 px além do alvo e volta — simula impacto físico do reel
                DOTween.Sequence()
                    .Append(_strip.DOAnchorPosY(-_bounceOvershoot, 0.07f).SetEase(Ease.OutQuad))
                    .Append(_strip.DOAnchorPosY(_restY, 0.07f).SetEase(Ease.InQuad))
                    .OnComplete(() => _stopTcs?.TrySetResult())
                    .OnKill(() => _stopTcs?.TrySetResult());
            })
            .OnKill(() => _stopTcs?.TrySetResult()); // segurança: resolve se destruído

        return _stopTcs.Task;
    }

    /// <summary>Exibe símbolos aleatórios (sem Blank) nos 3 slots de resultado.</summary>
    public void ShowRandom()
    {
        RandomizeRange(0, 3);
    }

    /// <summary>Exibe o prefab Blank nos 3 slots de resultado.</summary>
    public void ShowBlank()
    {
        for (int i = 0; i < 3; i++)
            SetSymbol(i, 8); // symbolId 8 = Blank
    }

    /// <summary>Para qualquer tween ativo imediatamente.</summary>
    public void KillSpin()
    {
        _spinTween?.Kill(); // dispara OnKill → resolve _stopTcs se pendente
        _spinTween = null;
    }
    /// <summary>
    /// Retorna o GameObject instanciado no slot de resultado indicado (row 0-2).
    /// Usado pela view para animar símbolos vencedores com DOTween.
    /// </summary>
    public GameObject GetResultInstance(int row)
    {
        if (row < 0 || row >= 3 || _instances == null || row >= _instances.Length) return null;
        return _instances[row];
    }
    // ═════════════════════════════════════════════════════════════════════════
    //  Internos
    // ═════════════════════════════════════════════════════════════════════════

    private void RandomizeRange(int startIndex, int count)
    {
        for (int i = 0; i < count; i++)
            // Exclui Blank (id=8) da rotação de spin para visual mais atraente
            SetSymbol(startIndex + i, Random.Range(0, 8));
    }

    private void SetSlots(int startIndex, int[] symbolIds)
    {
        for (int i = 0; i < symbolIds?.Length; i++)
            SetSymbol(startIndex + i, symbolIds[i]);
    }

    /// <summary>
    /// Destrói a instância atual do container e instancia o prefab do símbolo.
    /// </summary>
    private void SetSymbol(int containerIdx, int symbolId)
    {
        if (_slotContainers == null || containerIdx >= _slotContainers.Length) return;

        var container = _slotContainers[containerIdx];
        if (container == null) return;

        // Remove instância anterior — SetActive(false) oculta imediatamente;
        // Destroy() é diferido mas o objeto já não renderiza no mesmo frame.
        if (_instances != null && containerIdx < _instances.Length && _instances[containerIdx] != null)
        {
            _instances[containerIdx].SetActive(false);
            Destroy(_instances[containerIdx]);
            _instances[containerIdx] = null;
        }

        var prefab = _library?.Get(symbolId);
        if (prefab == null) return;

        // Instancia e posiciona dentro do container
        var go = Instantiate(prefab, container);
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale       = Vector3.one;
        }

        if (_instances != null) _instances[containerIdx] = go;
    }

    private void DestroyAllInstances()
    {
        if (_instances == null) return;
        foreach (var go in _instances)
            if (go != null) Destroy(go);
    }
}
}
