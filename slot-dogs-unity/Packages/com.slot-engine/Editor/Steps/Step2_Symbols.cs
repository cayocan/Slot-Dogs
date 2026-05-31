#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SlotEngine;

namespace SlotEngine.Editor
{
public class Step2_Symbols : IWizardStep
{
    public string Title => "Símbolos";

    private readonly List<bool> _foldouts = new List<bool>();

    public void Draw(SlotSetupData data)
    {
        EditorGUILayout.LabelField("Símbolos do Jogo", EditorStyles.boldLabel);

        while (_foldouts.Count < data.symbols.Length) _foldouts.Add(true);
        while (_foldouts.Count > data.symbols.Length) _foldouts.RemoveAt(_foldouts.Count - 1);

        for (int i = 0; i < data.symbols.Length; i++)
        {
            var sym   = data.symbols[i];
            var label = string.IsNullOrEmpty(sym.displayName) ? $"Símbolo {sym.symbolId}" : $"[{sym.symbolId}] {sym.displayName}";
            _foldouts[i] = EditorGUILayout.Foldout(_foldouts[i], label, true);

            if (_foldouts[i])
            {
                EditorGUI.indentLevel++;
                sym.symbolId    = EditorGUILayout.IntField("ID",    sym.symbolId);
                sym.displayName = EditorGUILayout.TextField("Nome", sym.displayName);
                sym.type        = (SymbolType)EditorGUILayout.EnumPopup("Tipo", sym.type);
                sym.sprite      = (UnityEngine.Sprite)EditorGUILayout.ObjectField("Sprite", sym.sprite, typeof(UnityEngine.Sprite), false);
                sym.useSpine    = EditorGUILayout.Toggle("Usar Spine", sym.useSpine);

                if (sym.useSpine)
                {
#if SPINE_EXISTS
                    sym.spineAsset = (Spine.Unity.SkeletonDataAsset)EditorGUILayout.ObjectField(
                        "Spine Asset", sym.spineAsset, typeof(Spine.Unity.SkeletonDataAsset), false);
#else
                    EditorGUILayout.HelpBox("Spine runtime não instalado. Instale o Spine for Unity e adicione o define SPINE_EXISTS.", MessageType.Warning);
#endif
                }

                data.symbols[i] = sym;
                EditorGUI.indentLevel--;

                if (GUILayout.Button($"Remover {label}", GUILayout.Width(160)))
                {
                    var list = new List<SymbolEntry>(data.symbols);
                    list.RemoveAt(i);
                    data.symbols = list.ToArray();
                    _foldouts.RemoveAt(i);
                    break;
                }
            }

            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("+ Adicionar Símbolo"))
        {
            var list = new List<SymbolEntry>(data.symbols);
            list.Add(new SymbolEntry { symbolId = list.Count, displayName = $"Simbolo{list.Count}" });
            data.symbols = list.ToArray();
            _foldouts.Add(true);
        }
    }

    public bool Validate(SlotSetupData data, out string errorMessage)
    {
        if (data.symbols == null || data.symbols.Length == 0)
        { errorMessage = "Adicione pelo menos um símbolo."; return false; }

        foreach (var s in data.symbols)
        {
            if (s.sprite == null)
            { errorMessage = $"Símbolo '{s.displayName}' (ID {s.symbolId}) não tem sprite."; return false; }
        }

        var ids = data.symbols.Select(s => s.symbolId).ToList();
        if (ids.Count != ids.Distinct().Count())
        { errorMessage = "Existem symbolIds duplicados."; return false; }

        errorMessage = null;
        return true;
    }
}
}
#endif
