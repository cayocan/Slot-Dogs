#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SlotEngine;

namespace SlotEngine.Editor
{
/// <summary>Gera prefabs de símbolo com Frame, Border e SymbolImage.</summary>
public static class SymbolPrefabGenerator
{
    private const float CellSize = 275f;

    /// <summary>Gera todos os prefabs e retorna o array na mesma ordem de symbols[].</summary>
    public static GameObject[] GenerateAll(SlotSetupData data, string prefabsPath)
    {
        EnsureFolder(prefabsPath);

        var prefabs = new GameObject[data.symbols.Length];
        for (int i = 0; i < data.symbols.Length; i++)
            prefabs[i] = GenerateOne(data.symbols[i], prefabsPath);

        return prefabs;
    }

    private static GameObject GenerateOne(SymbolEntry sym, string prefabsPath)
    {
        // ── Root ──────────────────────────────────────────────────────────
        var root   = new GameObject($"Symbol_{sym.symbolId}_{sym.displayName}");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(CellSize, CellSize);

        // ── Frame (stretch fill) ──────────────────────────────────────────
        var frame   = CreateImageChild(root, "Frame");
        var frameRt = frame.GetComponent<RectTransform>();
        frameRt.anchorMin        = Vector2.zero;
        frameRt.anchorMax        = Vector2.one;
        frameRt.sizeDelta        = Vector2.zero;
        frameRt.anchoredPosition = Vector2.zero;

        // ── Border (inativo — ativado em vitória pelo presenter do jogo) ──
        var border = CreateImageChild(root, "Border");
        border.SetActive(false);
        var borderRt = border.GetComponent<RectTransform>();
        borderRt.anchorMin        = new Vector2(0.5f, 0.5f);
        borderRt.anchorMax        = new Vector2(0.5f, 0.5f);
        borderRt.sizeDelta        = new Vector2(CellSize - 25f, CellSize - 25f);
        borderRt.anchoredPosition = Vector2.zero;

        // ── SymbolImage ───────────────────────────────────────────────────
        var symGo  = CreateImageChild(root, "SymbolImage");
        var symImg = symGo.GetComponent<Image>();
        symImg.sprite          = sym.sprite;
        symImg.preserveAspect  = true;
        var symRt = symGo.GetComponent<RectTransform>();
        symRt.anchorMin        = new Vector2(0.5f, 0.5f);
        symRt.anchorMax        = new Vector2(0.5f, 0.5f);
        symRt.sizeDelta        = new Vector2(CellSize - 25f, CellSize - 25f);
        symRt.anchoredPosition = Vector2.zero;

        // ── Salvar prefab e destruir o objeto temporário ──────────────────
        var path   = $"{prefabsPath}/Symbol_{sym.symbolId}_{sym.displayName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return prefab;
    }

    private static GameObject CreateImageChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<Image>();
        return go;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts   = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
}
#endif
