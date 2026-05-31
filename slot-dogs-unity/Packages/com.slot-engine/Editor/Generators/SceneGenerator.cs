#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SlotEngine;

namespace SlotEngine.Editor
{
/// <summary>Gera GameScene e MenuScene a partir de SlotSetupData.</summary>
public static class SceneGenerator
{
    // ── GameScene ─────────────────────────────────────────────────────────

    public static void GenerateGameScene(SlotSetupData data, SymbolLibrary library,
                                         SlotMachineConfig config, string scenesPath)
    {
        EnsureFolder(scenesPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        CreateEventSystem();

        var canvas = CreateCanvas("Canvas");

        var bg = CreateImage(canvas, "Background", data.gameBackground);
        StretchFill(bg.GetComponent<RectTransform>());

        var slotMachineGo = new GameObject("SlotMachine");
        slotMachineGo.transform.SetParent(canvas.transform, false);
        var slotRt = slotMachineGo.AddComponent<RectTransform>();
        StretchFill(slotRt);
        var view = slotMachineGo.AddComponent<GenericSlotMachineView>();

        var reelArea = new GameObject("ReelArea");
        reelArea.transform.SetParent(slotMachineGo.transform, false);
        reelArea.AddComponent<RectTransform>();

        var reelComponents = new ReelStrip[data.gridColumns];
        for (int col = 0; col < data.gridColumns; col++)
            reelComponents[col] = CreateReel(reelArea, col, data);

        var winPanel = CreatePanel(slotMachineGo, "WinPanel");
        winPanel.SetActive(false);
        var totalWinTmp = CreateTMPText(winPanel, "TotalWinText", "+0").GetComponent<TMP_Text>();

        var freeSpinPanel = CreatePanel(slotMachineGo, "FreeSpinPanel");
        freeSpinPanel.SetActive(false);
        var freeSpinsTmp = CreateTMPText(freeSpinPanel, "FreeSpinsText", "0").GetComponent<TMP_Text>();

        var buttonsGo = new GameObject("Buttons");
        buttonsGo.transform.SetParent(slotMachineGo.transform, false);
        buttonsGo.AddComponent<RectTransform>();

        Button spinBtn = null, autoSpinBtn = null, betUpBtn = null, betDownBtn = null;
        foreach (var entry in data.buttons)
        {
            var btn = CreateButton(buttonsGo, entry);
            switch (entry.role)
            {
                case ButtonRole.Spin:     spinBtn    = btn; break;
                case ButtonRole.AutoSpin: autoSpinBtn = btn; break;
                case ButtonRole.BetUp:    betUpBtn    = btn; break;
                case ButtonRole.BetDown:  betDownBtn  = btn; break;
            }
        }

        var coinsTmp   = CreateTMPText(slotMachineGo, "CoinsText",      "0").GetComponent<TMP_Text>();
        var betLineTmp = CreateTMPText(slotMachineGo, "BetPerLineText", "0").GetComponent<TMP_Text>();

        WireGenericView(view, reelComponents, library, winPanel, freeSpinPanel,
                 spinBtn, autoSpinBtn, betUpBtn, betDownBtn,
                 coinsTmp, betLineTmp, freeSpinsTmp, totalWinTmp);

        var scenePath = $"{scenesPath}/GameScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
    }

    // ── MenuScene ─────────────────────────────────────────────────────────

    public static void GenerateMenuScene(SlotSetupData data, string scenesPath)
    {
        if (!data.menuScene.generate) return;
        EnsureFolder(scenesPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        CreateEventSystem();
        var canvas = CreateCanvas("Canvas");

        var bg = CreateImage(canvas, "Background", data.menuScene.background);
        StretchFill(bg.GetComponent<RectTransform>());

        if (data.menuScene.logo != null)
        {
            var logo    = CreateImage(canvas, "Logo", data.menuScene.logo);
            var logoRt  = logo.GetComponent<RectTransform>();
            logoRt.anchorMin        = new Vector2(0.5f, 0.7f);
            logoRt.anchorMax        = new Vector2(0.5f, 0.7f);
            logoRt.sizeDelta        = new Vector2(400f, 200f);
            logoRt.anchoredPosition = Vector2.zero;
        }

        CreateButton(canvas, data.menuScene.playButton);

        var scenePath = $"{scenesPath}/MenuScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void CreateEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateCanvas(string name)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject CreateImage(GameObject parent, string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        return go;
    }

    private static GameObject CreatePanel(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateTMPText(GameObject parent, string name, string defaultText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = defaultText;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static Button CreateButton(GameObject parent, ButtonEntry entry)
    {
        var go = new GameObject(string.IsNullOrEmpty(entry.buttonId) ? "Button" : entry.buttonId);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        if (entry.normalSprite != null)
        {
            img.sprite     = entry.normalSprite;
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = entry.highlightSprite;
            ss.pressedSprite     = entry.pressedSprite;
            ss.disabledSprite    = entry.disabledSprite;
            btn.spriteState      = ss;
        }
        else
        {
            btn.transition = Selectable.Transition.ColorTint;
        }

        var label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        StretchFill(lrt);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text      = entry.buttonId;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private static ReelStrip CreateReel(GameObject parent, int col, SlotSetupData data)
    {
        const float cellH = 150f;
        const float cellW = 150f;

        var reelGo = new GameObject($"Reel_{col}");
        reelGo.transform.SetParent(parent.transform, false);
        var reelRt = reelGo.AddComponent<RectTransform>();
        reelRt.sizeDelta        = new Vector2(cellW, cellH * data.gridRows);
        reelRt.anchoredPosition = new Vector2(col * cellW, 0f);

        reelGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var mask = reelGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var strip = reelGo.AddComponent<ReelStrip>();

        var stripGo = new GameObject("Strip");
        stripGo.transform.SetParent(reelGo.transform, false);
        var stripRt = stripGo.AddComponent<RectTransform>();
        stripRt.pivot            = new Vector2(0.5f, 1f);
        stripRt.anchorMin        = new Vector2(0.5f, 1f);
        stripRt.anchorMax        = new Vector2(0.5f, 1f);
        stripRt.sizeDelta        = new Vector2(cellW, cellH * data.gridRows * 2f);
        stripRt.anchoredPosition = Vector2.zero;

        var containers = new RectTransform[data.gridRows * 2];
        for (int s = 0; s < data.gridRows * 2; s++)
        {
            var slotGo = new GameObject($"Slot_{s}");
            slotGo.transform.SetParent(stripGo.transform, false);
            var slotRt = slotGo.AddComponent<RectTransform>();
            slotRt.pivot            = new Vector2(0.5f, 1f);
            slotRt.anchorMin        = new Vector2(0.5f, 1f);
            slotRt.anchorMax        = new Vector2(0.5f, 1f);
            slotRt.sizeDelta        = new Vector2(cellW, cellH);
            slotRt.anchoredPosition = new Vector2(0f, -s * cellH);
            containers[s] = slotRt;
        }

        var so    = new SerializedObject(strip);
        so.FindProperty("_strip").objectReferenceValue = stripRt;
        var conts = so.FindProperty("_slotContainers");
        conts.arraySize = data.gridRows * 2;
        for (int s = 0; s < data.gridRows * 2; s++)
            conts.GetArrayElementAtIndex(s).objectReferenceValue = containers[s];
        so.FindProperty("_cellHeight").floatValue = cellH;
        so.ApplyModifiedPropertiesWithoutUndo();

        return strip;
    }

    /// <summary>
    /// Configura os campos serializados de <see cref="GenericSlotMachineView"/>.
    /// Intencionalmente usa o tipo concreto: o wizard padrão sempre gera essa view.
    /// Para views customizadas, sobrescreva este método em uma subclasse de SceneGenerator.
    /// </summary>
    private static void WireGenericView(GenericSlotMachineView view,
        ReelStrip[] reels, SymbolLibrary library,
        GameObject winPanel, GameObject freeSpinPanel,
        Button spinBtn, Button autoSpinBtn, Button betUpBtn, Button betDownBtn,
        TMP_Text coinsText, TMP_Text betText, TMP_Text freeSpinsText, TMP_Text totalWinText)
    {
        var so = new SerializedObject(view);

        var reelsProp = so.FindProperty("_reels");
        reelsProp.arraySize = reels.Length;
        for (int i = 0; i < reels.Length; i++)
            reelsProp.GetArrayElementAtIndex(i).objectReferenceValue = reels[i];

        so.FindProperty("_symbolLibrary").objectReferenceValue      = library;
        so.FindProperty("_winPanel").objectReferenceValue           = winPanel;
        so.FindProperty("_freeSpinPanel").objectReferenceValue      = freeSpinPanel;
        so.FindProperty("_spinButton").objectReferenceValue         = spinBtn;
        so.FindProperty("_autoSpinButton").objectReferenceValue     = autoSpinBtn;
        so.FindProperty("_betIncreaseButton").objectReferenceValue  = betUpBtn;
        so.FindProperty("_betDecreaseButton").objectReferenceValue  = betDownBtn;
        so.FindProperty("_coinsText").objectReferenceValue          = coinsText;
        so.FindProperty("_betPerLineText").objectReferenceValue     = betText;
        so.FindProperty("_freeSpinsText").objectReferenceValue      = freeSpinsText;
        so.FindProperty("_totalWinText").objectReferenceValue       = totalWinText;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
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
