/// <summary>
/// Marcador genérico para o resultado de um spin.
/// Implementado pelo DTO de cada jogo (ex.: <see cref="SpinResponse"/> no Slot Dogs).
/// A engine apenas transporta o resultado entre estados e View.
/// A View concreta faz cast para o tipo específico do jogo internamente.
/// </summary>
public interface ISpinResult { }
