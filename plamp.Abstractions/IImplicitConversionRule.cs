namespace plamp.Abstractions;

/// <summary>
/// Интерфейс, описывающий правила неявной конверсии типа в целевой.
/// Определяется языком. Зачастую служит, чтобы описать правила неявного приведения числовых типов.
/// </summary>
public interface IImplicitConversionRule
{
    /// <summary>
    /// Целевой тип, к которому намереваемся привести искомый
    /// </summary>
    ICompileTimeType ApplicableForTargetType { get; }

    /// <summary>
    /// Возможно ли приведение данного типа к целевому.
    /// </summary>
    /// <param name="type">Искомый тип</param>
    /// <returns>Флаг, говорящий о (не)возможности приведения типа.</returns>
    public bool Convertable(ICompileTimeType type);
}