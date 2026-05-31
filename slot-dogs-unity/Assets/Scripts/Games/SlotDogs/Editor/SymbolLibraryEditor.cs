#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotEngine;
using SlotEngine.Editor;

/// <summary>
/// Editor customizado para <see cref="SymbolLibrary"/>.
/// Exibe cada entrada com label colorida mostrando o nome do símbolo,
/// detecta duplicatas e entradas ausentes, e oferece botão de população automática.
/// </summary>
[CustomEditor(typeof(SymbolLibrary))]
public class SymbolLibraryEditor : Editor
{
    // ─── Símbolos conhecidos do jogo ──────────────────────────────────────────
    private static readonly (int id, string name)[] KnownSymbols =
    {
        (0, "Husky"),
        (1, "Golden"),
        (2, "Shiba"),
        (3, "Pug"),
        (4, "Beagle"),
        (5, "Dachshund"),
        (6, "Wild"),
        (7, "Scatter"),
        (8, "Blank"),
    };

    private static readonly Color ColorOk      = new Color(0.55f, 0.85f, 0.55f);
    private static readonly Color ColorWarning = new Color(1.00f, 0.85f, 0.35f);
    private static readonly Color ColorError   = new Color(0.90f, 0.40f, 0.40f);

    // ─────────────────────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var entriesProp = serializedObject.FindProperty("_entries");

        // ── Validação global ─────────────────────────────────────────────────
        DrawValidationMessages(entriesProp);

        EditorGUILayout.Space(4);

        // ── Entradas ─────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Entradas", EditorStyles.boldLabel);

        for (int i = 0; i < entriesProp.arraySize; i++)
            DrawEntry(entriesProp, i);

        // ── Botões de ação ───────────────────────────────────────────────────
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Entrada"))
            entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);

        using (new EditorGUI.DisabledScope(entriesProp.arraySize == 0))
        {
            if (GUILayout.Button("− Última"))
                entriesProp.DeleteArrayElementAtIndex(entriesProp.arraySize - 1);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Preencher Entradas Padrão (0–8)"))
            PopulateDefaults(entriesProp);

        serializedObject.ApplyModifiedProperties();
    }

    // ─── Desenha uma entrada individual ──────────────────────────────────────
    private void DrawEntry(SerializedProperty entriesProp, int index)
    {
        var entry       = entriesProp.GetArrayElementAtIndex(index);
        var idProp      = entry.FindPropertyRelative("symbolId");
        var nameProp    = entry.FindPropertyRelative("displayName");
        var prefabProp  = entry.FindPropertyRelative("prefab");

        int   sid       = idProp.intValue;
        bool  prefabSet = prefabProp.objectReferenceValue != null;
        Color boxColor  = prefabSet ? ColorOk : ColorError;

        // Fundo colorido
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = boxColor;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = prevBg;

        // Cabeçalho da entrada
        string knownName  = KnownName(sid);
        string headerText = knownName != null
            ? $"[{sid}]  {knownName}"
            : $"[{sid}]  (desconhecido)";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);

        // Botão de remover
        if (GUILayout.Button("✕", GUILayout.Width(22)))
        {
            entriesProp.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        // Campos
        EditorGUILayout.PropertyField(idProp,     new GUIContent("Symbol ID"));
        EditorGUILayout.PropertyField(nameProp,   new GUIContent("Display Name"));
        EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    // ─── Valida duplicatas e símbolos ausentes ────────────────────────────────
    private static void DrawValidationMessages(SerializedProperty entriesProp)
    {
        // Conta ocorrências de cada id
        var counts = new System.Collections.Generic.Dictionary<int, int>();
        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            int id = entriesProp
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("symbolId").intValue;
            counts.TryGetValue(id, out int c);
            counts[id] = c + 1;
        }

        // Duplicatas
        foreach (var kv in counts)
            if (kv.Value > 1)
                EditorGUILayout.HelpBox(
                    $"symbolId {kv.Key} ({KnownName(kv.Key) ?? "?"}) aparece {kv.Value} vezes.",
                    MessageType.Error);

        // Ausentes (0–8)
        var missing = new System.Collections.Generic.List<string>();
        foreach (var (id, name) in KnownSymbols)
            if (!counts.ContainsKey(id)) missing.Add($"{id}={name}");

        if (missing.Count > 0)
            EditorGUILayout.HelpBox(
                "Faltando: " + string.Join(", ", missing),
                MessageType.Warning);
    }

    // ─── Adiciona entradas padrão para IDs ainda ausentes ────────────────────
    private static void PopulateDefaults(SerializedProperty entriesProp)
    {
        // Coleta IDs já existentes
        var existing = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < entriesProp.arraySize; i++)
            existing.Add(entriesProp
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("symbolId").intValue);

        foreach (var (id, name) in KnownSymbols)
        {
            if (existing.Contains(id)) continue;

            int idx = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(idx);
            var newEntry = entriesProp.GetArrayElementAtIndex(idx);
            newEntry.FindPropertyRelative("symbolId").intValue      = id;
            newEntry.FindPropertyRelative("displayName").stringValue = name;
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = null;
        }
    }

    private static string KnownName(int id)
    {
        foreach (var (sid, name) in KnownSymbols)
            if (sid == id) return name;
        return null;
    }
}
#endif
