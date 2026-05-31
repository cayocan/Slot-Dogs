#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotEngine;

namespace SlotEngine.Editor
{
public class Step1_GeneralConfig : IWizardStep
{
    public string Title => "Configuração Geral";

    public void Draw(SlotSetupData data)
    {
        EditorGUILayout.LabelField("Identidade", EditorStyles.boldLabel);
        data.gameName   = EditorGUILayout.TextField("Nome do Jogo",   data.gameName);
        data.outputPath = EditorGUILayout.TextField("Pasta de Saída", data.outputPath);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        data.gridColumns = EditorGUILayout.IntField("Colunas (Reels)", data.gridColumns);
        data.gridRows    = EditorGUILayout.IntField("Linhas",          data.gridRows);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Aposta", EditorStyles.boldLabel);
        data.paylineCount    = EditorGUILayout.IntField("Paylines",             data.paylineCount);
        data.minBet          = EditorGUILayout.IntField("Aposta Mínima",        data.minBet);
        data.maxBet          = EditorGUILayout.IntField("Aposta Máxima",        data.maxBet);
        data.minSpinDuration = EditorGUILayout.FloatField("Duração Mín. Spin (s)", data.minSpinDuration);
    }

    public bool Validate(SlotSetupData data, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(data.gameName))
        { errorMessage = "Nome do jogo não pode estar vazio."; return false; }

        if (data.gridColumns < 1)
        { errorMessage = "Mínimo 1 coluna."; return false; }

        if (data.gridRows < 1)
        { errorMessage = "Mínimo 1 linha."; return false; }

        if (data.minBet > data.maxBet)
        { errorMessage = "Aposta mínima não pode ser maior que a máxima."; return false; }

        errorMessage = null;
        return true;
    }
}
}
#endif
