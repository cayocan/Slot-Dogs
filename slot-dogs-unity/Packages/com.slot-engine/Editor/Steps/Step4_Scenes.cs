#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotEngine;

namespace SlotEngine.Editor
{
public class Step4_Scenes : IWizardStep
{
    public string Title => "Cenas";

    public void Draw(SlotSetupData data)
    {
        EditorGUILayout.LabelField("Configuração de Cenas", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("A GameScene sempre será gerada.", MessageType.Info);
        EditorGUILayout.Space();

        data.menuScene.generate = EditorGUILayout.Toggle("Gerar MenuScene", data.menuScene.generate);

        if (data.menuScene.generate)
        {
            EditorGUI.indentLevel++;
            data.menuScene.background = (Sprite)EditorGUILayout.ObjectField("Background", data.menuScene.background, typeof(Sprite), false);
            data.menuScene.logo       = (Sprite)EditorGUILayout.ObjectField("Logo",       data.menuScene.logo,       typeof(Sprite), false);

            EditorGUILayout.LabelField("Botão Play", EditorStyles.boldLabel);
            var pb = data.menuScene.playButton;
            pb.normalSprite    = (Sprite)EditorGUILayout.ObjectField("Normal",      pb.normalSprite,    typeof(Sprite), false);
            pb.highlightSprite = (Sprite)EditorGUILayout.ObjectField("Highlighted", pb.highlightSprite, typeof(Sprite), false);
            pb.pressedSprite   = (Sprite)EditorGUILayout.ObjectField("Pressed",     pb.pressedSprite,   typeof(Sprite), false);
            pb.disabledSprite  = (Sprite)EditorGUILayout.ObjectField("Disabled",    pb.disabledSprite,  typeof(Sprite), false);
            data.menuScene.playButton = pb;
            EditorGUI.indentLevel--;
        }
    }

    public bool Validate(SlotSetupData data, out string errorMessage)
    {
        if (data.menuScene.generate && data.menuScene.background == null)
        { errorMessage = "Background da MenuScene é obrigatório quando 'Gerar MenuScene' está ativo."; return false; }

        errorMessage = null;
        return true;
    }
}
}
#endif
