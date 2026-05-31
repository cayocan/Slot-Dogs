using System;
using UnityEngine;

namespace SlotEngine
{
/// <summary>
/// Configuração persistente do Game Setup Wizard.
/// Criação: Assets → Create → Slot Engine → Game Setup
/// </summary>
[CreateAssetMenu(fileName = "SlotSetupData", menuName = "Slot Engine/Game Setup")]
public class SlotSetupData : ScriptableObject
{
    [Header("Configuração Geral")]
    public string gameName        = "MySlotGame";
    public string outputPath      = "Assets/MySlotGame/";
    public int    gridColumns     = 5;
    public int    gridRows        = 3;
    public int    paylineCount    = 15;
    public int    minBet          = 1;
    public int    maxBet          = 100;
    public float  minSpinDuration = 1.5f;

    [Header("Símbolos")]
    public SymbolEntry[] symbols = Array.Empty<SymbolEntry>();

    [Header("Botões e Background")]
    public ButtonEntry[] buttons        = Array.Empty<ButtonEntry>();
    public Sprite        gameBackground;

    [Header("Menu Scene")]
    public MenuSceneSetup menuScene = new MenuSceneSetup();
}

public enum SymbolType { Normal, Wild, Scatter, Blank }
public enum ButtonRole { Spin, AutoSpin, BetUp, BetDown, Custom }

[Serializable]
public class SymbolEntry
{
    public int        symbolId;
    public string     displayName;
    public SymbolType type;
    public Sprite     sprite;
    public bool       useSpine;
#if SPINE_EXISTS
    public Spine.Unity.SkeletonDataAsset spineAsset;
#endif
}

[Serializable]
public class ButtonEntry
{
    public string     buttonId;
    public ButtonRole role;
    public Sprite     normalSprite;
    public Sprite     highlightSprite;
    public Sprite     pressedSprite;
    public Sprite     disabledSprite;
}

[Serializable]
public class MenuSceneSetup
{
    public bool        generate;
    public Sprite      background;
    public Sprite      logo;
    public ButtonEntry playButton = new ButtonEntry { buttonId = "Play", role = ButtonRole.Custom };
}
}
