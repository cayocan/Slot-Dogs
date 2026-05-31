using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotEngine
{
/// <summary>
/// Abstrai a comunicação com o backend para execução de um spin.
/// Implemente esta interface em cada jogo específico (ex.: SlotDogsSpinProvider).
/// Os estados da engine chamam apenas este contrato — sem dependência de SessionPresenter.
/// </summary>
public interface ISpinProvider
{
    /// <summary>
    /// Executa um spin no backend.
    /// O resultado deve ser gravado no <see cref="SessionModel"/> pelo implementador,
    /// de forma que <see cref="SessionModel.LastSpin"/> fique disponível ao retornar.
    /// </summary>
    /// <returns>true em caso de sucesso; false em caso de erro de rede ou API.</returns>
    UniTask<bool> SpinAsync(int betPerLine, CancellationToken ct = default);
}
}
