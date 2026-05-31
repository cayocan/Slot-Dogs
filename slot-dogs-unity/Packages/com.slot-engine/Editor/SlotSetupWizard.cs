#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotEngine;
using SlotEngine.Editor;

namespace SlotEngine.Editor
{
/// <summary>
/// Wizard multi-step para configurar e gerar um novo jogo de slot machine.
/// Acesso: Tools → Slot Engine → Game Setup Wizard
/// </summary>
public class SlotSetupWizard : EditorWindow
{
    [MenuItem("Tools/Slot Engine/Game Setup Wizard")]
    public static void Open()
    {
        var wnd = GetWindow<SlotSetupWizard>("Slot Setup Wizard");
        wnd.minSize = new Vector2(480f, 520f);
    }

    // ── Estado ────────────────────────────────────────────────────────────
    private SlotSetupData _data;
    private int           _stepIndex;
    private string        _validationError;
    private Vector2       _scrollPos;

    private readonly IWizardStep[] _steps =
    {
        new Step1_GeneralConfig(),
        new Step2_Symbols(),
        new Step3_Buttons(),
        new Step4_Scenes(),
        new Step5_Summary()
    };

    // ── GUI ───────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        _data = (SlotSetupData)EditorGUILayout.ObjectField("Setup Asset", _data, typeof(SlotSetupData), false);
        if (EditorGUI.EndChangeCheck()) _validationError = null;

        if (_data == null)
        {
            EditorGUILayout.HelpBox(
                "Selecione ou crie um SlotSetupData: Assets → Create → Slot Engine → Game Setup.",
                MessageType.Info);

            if (GUILayout.Button("Criar novo SlotSetupData"))
                CreateNewData();
            return;
        }

        EditorGUILayout.Space(4);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        EditorGUI.BeginChangeCheck();
        _steps[_stepIndex].Draw(_data);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_data);
            _validationError = null;
        }
        EditorGUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(_validationError))
            EditorGUILayout.HelpBox(_validationError, MessageType.Error);

        EditorGUILayout.Space(8);
        DrawFooter();
    }

    // ── Header ────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("SLOT ENGINE — GAME SETUP WIZARD", EditorStyles.boldLabel);

        var rect = GUILayoutUtility.GetRect(position.width, 18f);
        float progress = (float)(_stepIndex + 1) / _steps.Length;
        EditorGUI.ProgressBar(rect, progress,
            $"Passo {_stepIndex + 1} de {_steps.Length}: {_steps[_stepIndex].Title}");
    }

    // ── Footer ────────────────────────────────────────────────────────────

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = _stepIndex > 0;
        if (GUILayout.Button("← Voltar", GUILayout.Height(28)))
        {
            _stepIndex--;
            _validationError = null;
        }
        GUI.enabled = true;

        bool isLastStep = _stepIndex == _steps.Length - 1;
        if (isLastStep)
        {
            if (GUILayout.Button("Gerar Jogo", GUILayout.Height(28)))
                TryGenerate();
        }
        else
        {
            if (GUILayout.Button("Próximo →", GUILayout.Height(28)))
            {
                if (_steps[_stepIndex].Validate(_data, out var err))
                {
                    _stepIndex++;
                    _validationError = null;
                }
                else
                {
                    _validationError = err;
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── Geração ───────────────────────────────────────────────────────────

    private void TryGenerate()
    {
        for (int i = 0; i < _steps.Length; i++)
        {
            if (!_steps[i].Validate(_data, out var err))
            {
                _validationError = $"[Passo {i + 1}] {err}";
                _stepIndex = i;
                return;
            }
        }

        AssetDatabase.SaveAssets();

        var root        = _data.outputPath.TrimEnd('/');
        var dataPath    = root + "/Data";
        var prefabsPath = root + "/Prefabs/Symbols";
        var scenesPath  = root + "/Scenes";

        try
        {
            EditorUtility.DisplayProgressBar("Gerando jogo...", "Criando assets...", 0.1f);
            var config  = AssetGenerator.CreateConfig(_data, dataPath);
            var library = AssetGenerator.CreateSymbolLibrary(dataPath);

            EditorUtility.DisplayProgressBar("Gerando jogo...", "Criando prefabs de símbolo...", 0.3f);
            var prefabs = SymbolPrefabGenerator.GenerateAll(_data, prefabsPath);

            EditorUtility.DisplayProgressBar("Gerando jogo...", "Populando SymbolLibrary...", 0.5f);
            AssetGenerator.PopulateSymbolLibrary(library, _data.symbols, prefabs);

            EditorUtility.DisplayProgressBar("Gerando jogo...", "Gerando GameScene...", 0.7f);
            SceneGenerator.GenerateGameScene(_data, library, config, scenesPath);

            if (_data.menuScene.generate)
            {
                EditorUtility.DisplayProgressBar("Gerando jogo...", "Gerando MenuScene...", 0.9f);
                SceneGenerator.GenerateMenuScene(_data, scenesPath);
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Geração concluída!",
                $"Projeto '{_data.gameName}' gerado em:\n{_data.outputPath}", "OK");

            var asset = AssetDatabase.LoadAssetAtPath<Object>(_data.outputPath);
            if (asset != null) Selection.activeObject = asset;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ── Utilitários ───────────────────────────────────────────────────────

    private void CreateNewData()
    {
        var path = EditorUtility.SaveFilePanelInProject(
            "Novo SlotSetupData", "SlotSetupData", "asset",
            "Escolha onde salvar o asset de configuração");

        if (string.IsNullOrEmpty(path)) return;

        _data = ScriptableObject.CreateInstance<SlotSetupData>();
        AssetDatabase.CreateAsset(_data, path);
        AssetDatabase.SaveAssets();
    }
}
}
#endif
