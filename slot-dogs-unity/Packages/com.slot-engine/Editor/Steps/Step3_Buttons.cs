#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SlotEngine;

namespace SlotEngine.Editor
{
public class Step3_Buttons : IWizardStep
{
    public string Title => "Botões e Background";

    private readonly List<bool> _foldouts = new List<bool>();

    public void Draw(SlotSetupData data)
    {
        EditorGUILayout.LabelField("Background da GameScene", EditorStyles.boldLabel);
        data.gameBackground = (Sprite)EditorGUILayout.ObjectField("Background", data.gameBackground, typeof(Sprite), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Botões", EditorStyles.boldLabel);

        while (_foldouts.Count < data.buttons.Length) _foldouts.Add(true);
        while (_foldouts.Count > data.buttons.Length) _foldouts.RemoveAt(_foldouts.Count - 1);

        for (int i = 0; i < data.buttons.Length; i++)
        {
            var btn   = data.buttons[i];
            var label = string.IsNullOrEmpty(btn.buttonId) ? $"Botão {i}" : btn.buttonId;

            _foldouts[i] = EditorGUILayout.Foldout(_foldouts[i], label, true);
            if (_foldouts[i])
            {
                EditorGUI.indentLevel++;
                btn.buttonId        = EditorGUILayout.TextField("ID/Label",           btn.buttonId);
                btn.role            = (ButtonRole)EditorGUILayout.EnumPopup("Função", btn.role);
                btn.normalSprite    = (Sprite)EditorGUILayout.ObjectField("Normal",      btn.normalSprite,    typeof(Sprite), false);
                btn.highlightSprite = (Sprite)EditorGUILayout.ObjectField("Highlighted", btn.highlightSprite, typeof(Sprite), false);
                btn.pressedSprite   = (Sprite)EditorGUILayout.ObjectField("Pressed",     btn.pressedSprite,   typeof(Sprite), false);
                btn.disabledSprite  = (Sprite)EditorGUILayout.ObjectField("Disabled",    btn.disabledSprite,  typeof(Sprite), false);
                data.buttons[i] = btn;
                EditorGUI.indentLevel--;

                if (GUILayout.Button($"Remover {label}", GUILayout.Width(160)))
                {
                    var list = new List<ButtonEntry>(data.buttons);
                    list.RemoveAt(i);
                    data.buttons = list.ToArray();
                    _foldouts.RemoveAt(i);
                    break;
                }
            }
            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("+ Adicionar Botão"))
        {
            var list = new List<ButtonEntry>(data.buttons);
            list.Add(new ButtonEntry { buttonId = "NovoBotao", role = ButtonRole.Custom });
            data.buttons = list.ToArray();
            _foldouts.Add(true);
        }
    }

    public bool Validate(SlotSetupData data, out string errorMessage)
    {
        if (data.gameBackground == null)
        { errorMessage = "Background da GameScene é obrigatório."; return false; }

        bool hasSpinButton = false;
        if (data.buttons != null)
            foreach (var b in data.buttons)
                if (b.role == ButtonRole.Spin) { hasSpinButton = true; break; }

        if (!hasSpinButton)
        { errorMessage = "Adicione pelo menos um botão com função 'Spin'."; return false; }

        errorMessage = null;
        return true;
    }
}
}
#endif
