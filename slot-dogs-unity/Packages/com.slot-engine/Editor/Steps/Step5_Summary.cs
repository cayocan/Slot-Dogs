#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotEngine;

namespace SlotEngine.Editor
{
public class Step5_Summary : IWizardStep
{
    public string Title => "Resumo";

    public void Draw(SlotSetupData data)
    {
        EditorGUILayout.LabelField("O que será gerado:", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"  Pasta de saída:  {data.outputPath}");
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Assets:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  SlotMachineConfig.asset");
        EditorGUILayout.LabelField("  SymbolLibrary.asset");
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField($"Prefabs de símbolo ({data.symbols?.Length ?? 0}):", EditorStyles.boldLabel);
        if (data.symbols != null)
            foreach (var s in data.symbols)
                EditorGUILayout.LabelField($"  Symbol_{s.symbolId}_{s.displayName}.prefab");
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Cenas:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  GameScene.unity");
        if (data.menuScene.generate)
            EditorGUILayout.LabelField("  MenuScene.unity");
    }

    public bool Validate(SlotSetupData data, out string errorMessage)
    {
        errorMessage = null;
        return true;
    }
}
}
#endif
