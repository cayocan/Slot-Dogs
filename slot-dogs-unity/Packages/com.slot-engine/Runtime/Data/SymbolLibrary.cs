using System;
using UnityEngine;

namespace SlotEngine
{
/// <summary>
/// Asset que mapeia cada symbolId ao seu prefab de UI.
/// Criação: Assets → Create → Slot Engine → Symbol Library
///
/// Uso: arraste o asset para o campo _symbolLibrary da SlotMachineView.
/// Não há restrição de ordem — cada entrada carrega seu próprio symbolId.
/// </summary>
[CreateAssetMenu(fileName = "SymbolLibrary", menuName = "Slot Engine/Symbol Library")]
public class SymbolLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("ID do símbolo — deve coincidir com os valores de SymbolId em Dtos.cs")]
        public int        symbolId;

        [Tooltip("Nome exibido no Inspector; não afeta o funcionamento em runtime")]
        public string     displayName;

        [Tooltip("Prefab instanciado no slot quando este símbolo é exibido")]
        public GameObject prefab;
    }

    [SerializeField] private Entry[] _entries;

    // ─────────────────────────────────────────────────────────────────────────
    //  API Runtime
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna o prefab associado ao symbolId.
    /// Retorna null e loga aviso se não encontrado.
    /// </summary>
    public GameObject Get(int symbolId)
    {
        if (_entries != null)
            foreach (var e in _entries)
                if (e.symbolId == symbolId) return e.prefab;

        Debug.LogWarning($"[SymbolLibrary] Prefab não encontrado para symbolId={symbolId} em '{name}'");
        return null;
    }

    /// <summary>
    /// Retorna true se todos os symbolIds de 0 a <paramref name="count"/>−1 possuem prefab configurado.
    /// Útil para validação no Editor.
    /// </summary>
    public bool IsComplete(int count = 9)
    {
        for (int id = 0; id < count; id++)
        {
            bool found = false;
            if (_entries != null)
                foreach (var e in _entries)
                    if (e.symbolId == id && e.prefab != null) { found = true; break; }
            if (!found) return false;
        }
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Acesso interno (usado pelo SymbolLibraryEditor)
    // ─────────────────────────────────────────────────────────────────────────

    public Entry[] Entries => _entries;
}
}
