using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotEngine
{
// ═══════════════════════════════════════════════════════════════════════════════
//  Slot Dogs — AudioManager
//  Singleton persistente (DontDestroyOnLoad).
//  Gerencia um pool de AudioSources e permite tocar/parar áudios por nome,
//  com controle de pitch no momento do Play.
//
//  Setup na cena:
//    1. Crie um GameObject "AudioManager" na MenuScene (ou cena raiz).
//    2. Adicione este script a ele.
//    3. Preencha _entries no Inspector com nome + AudioClip + volume + loop.
//    4. Chame AudioManager.Instance.Play("nome") em qualquer script.
// ═══════════════════════════════════════════════════════════════════════════════

[Serializable]
public class AudioEntry
{
    [Tooltip("Identificador usado em AudioManager.Play(\"nome\")")]
    public string    name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float     volume = 1f;
    public bool      loop   = false;
}

public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Entradas de Áudio")]
    [SerializeField] private AudioEntry[] _entries;

    [Header("Pool")]
    [Tooltip("Número máximo de sons simultâneos")]
    [SerializeField] private int _poolSize = 8;

    // ── Internos ──────────────────────────────────────────────────────────────

    private AudioSource[]               _pool;
    private Dictionary<string, AudioEntry> _lookup;

    // ═════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida
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

        BuildLookup();
        BuildPool();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API pública
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toca o áudio registrado com <paramref name="audioName"/>.
    /// </summary>
    /// <param name="audioName">Nome cadastrado em _entries (case-insensitive).</param>
    /// <param name="pitch">Pitch de reprodução. 1 = normal, 2 = oitava acima.</param>
    public void Play(string audioName, float pitch = 1f)
    {
        if (!_lookup.TryGetValue(audioName, out var entry))
        {
            Debug.LogWarning($"[AudioManager] Áudio '{audioName}' não encontrado.");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"[AudioManager] AudioClip de '{audioName}' está nulo.");
            return;
        }

        var src = GetFreeSource();
        if (src == null) return;

        src.clip   = entry.clip;
        src.volume = entry.volume;
        src.loop   = entry.loop;
        src.pitch  = pitch;
        src.Play();
    }

    /// <summary>
    /// Para todos os AudioSources que estejam reproduzindo o áudio indicado.
    /// </summary>
    public void Stop(string audioName)
    {
        if (!_lookup.TryGetValue(audioName, out var entry)) return;

        foreach (var src in _pool)
            if (src.isPlaying && src.clip == entry.clip)
                src.Stop();
    }

    /// <summary>Para todos os áudios em reprodução.</summary>
    public void StopAll()
    {
        foreach (var src in _pool)
            src.Stop();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Internos
    // ═════════════════════════════════════════════════════════════════════════

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, AudioEntry>(StringComparer.OrdinalIgnoreCase);
        if (_entries == null) return;

        foreach (var e in _entries)
        {
            if (string.IsNullOrEmpty(e.name)) continue;
            if (_lookup.ContainsKey(e.name))
            {
                Debug.LogWarning($"[AudioManager] Nome duplicado ignorado: '{e.name}'");
                continue;
            }
            _lookup[e.name] = e;
        }
    }

    private void BuildPool()
    {
        _pool = new AudioSource[_poolSize];
        for (int i = 0; i < _poolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _pool[i] = src;
        }
    }

    /// <summary>
    /// Retorna um AudioSource livre (não tocando). Se todos estiverem ocupados,
    /// reutiliza o primeiro slot (política "oldest overwrite").
    /// </summary>
    private AudioSource GetFreeSource()
    {
        foreach (var src in _pool)
            if (!src.isPlaying) return src;

        // Fallback: sobrescreve o slot 0
        return _pool.Length > 0 ? _pool[0] : null;
    }
}
}
