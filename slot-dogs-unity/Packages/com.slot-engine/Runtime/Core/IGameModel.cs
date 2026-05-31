using System;

namespace SlotEngine
{
/// <summary>
/// Estado de sessão exposto à engine da slot machine.
/// Implementado pelo model de cada jogo (ex.: <see cref="SessionModel"/> no Slot Dogs).
/// A engine lê apenas Coins, FreeSpinsRemaining e LastSpin.
/// </summary>
public interface IGameModel
{
    /// <summary>Saldo atual de moedas do jogador.</summary>
    int Coins { get; }

    /// <summary>Quantidade de free spins ainda disponíveis.</summary>
    int FreeSpinsRemaining { get; }

    /// <summary>Resultado do último spin processado. Null antes do primeiro spin.</summary>
    ISpinResult LastSpin { get; }

    /// <summary>Disparado quando o saldo de moedas muda.</summary>
    event Action<int> OnCoinsChanged;
}
}
