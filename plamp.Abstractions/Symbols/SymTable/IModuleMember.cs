namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Общий интерфейс для всего, что объявлено напрямую в модуле
/// </summary>
public interface IModuleMember
{
    /// <summary>
    /// Имя модуля типа, модуль должен быть уникален в контексте компиляции.
    /// </summary>
    public string ModuleName { get; } 
}