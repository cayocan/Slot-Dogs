using UnityEngine;

/// <summary>
/// Configuração de uma slot machine — criada como asset no Project e
/// atribuída ao <see cref="SlotMachinePresenter"/> via Inspector.
/// Para um novo jogo, basta criar um novo asset com seus próprios valores.
///
/// Criação: Assets → Create → Slot Engine → Machine Config
/// </summary>
[CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Slot Engine/Machine Config")]
public class SlotMachineConfig : ScriptableObject
{
    [Tooltip("Deve coincidir com PAYLINES.length no backend (config.js)")]
    public int   paylineCount    = 15;

    [Tooltip("Aposta mínima por linha")]
    public int   minBet          = 1;

    [Tooltip("Aposta máxima por linha")]
    public int   maxBet          = 100;

    [Tooltip("Duração mínima da animação de giro em segundos")]
    public float minSpinDuration = 1.5f;
}
