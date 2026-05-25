/// <summary>
/// Contrato de um estado da slot machine.
/// Cada estado encapsula o comportamento de uma fase específica do jogo.
/// </summary>
public interface ISlotState
{
    /// <summary>Nome do estado — usado para logs e guardas de transição.</summary>
    string Name { get; }

    /// <summary>Chamado imediatamente ao entrar no estado.</summary>
    void Enter();

    /// <summary>Chamado imediatamente antes de sair do estado.</summary>
    void Exit();
}
