#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotEngine;

namespace SlotEngine.Editor
{
/// <summary>Cria SlotMachineConfig.asset e SymbolLibrary.asset na pasta de saída.</summary>
public static class AssetGenerator
{
    public static SlotMachineConfig CreateConfig(SlotSetupData data, string dataPath)
    {
        var config = ScriptableObject.CreateInstance<SlotMachineConfig>();
        config.paylineCount    = data.paylineCount;
        config.minBet          = data.minBet;
        config.maxBet          = data.maxBet;
        config.minSpinDuration = data.minSpinDuration;

        AssetDatabase.CreateAsset(config, $"{dataPath}/SlotMachineConfig.asset");
        return config;
    }

    public static SymbolLibrary CreateSymbolLibrary(string dataPath)
    {
        var library = ScriptableObject.CreateInstance<SymbolLibrary>();
        AssetDatabase.CreateAsset(library, $"{dataPath}/SymbolLibrary.asset");
        return library;
    }

    /// <summary>Popula a SymbolLibrary com os prefabs gerados.</summary>
    public static void PopulateSymbolLibrary(SymbolLibrary library, SymbolEntry[] symbols, GameObject[] prefabs)
    {
        var so      = new SerializedObject(library);
        var entries = so.FindProperty("_entries");
        entries.arraySize = symbols.Length;

        for (int i = 0; i < symbols.Length; i++)
        {
            var elem = entries.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("symbolId").intValue            = symbols[i].symbolId;
            elem.FindPropertyRelative("displayName").stringValue      = symbols[i].displayName;
            elem.FindPropertyRelative("prefab").objectReferenceValue  = prefabs[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }
}
}
#endif
