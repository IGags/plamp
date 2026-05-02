namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Общий интерфейс для всех объявлений верхнего уровня внутри модуля.
/// </summary>
public interface IModuleMember
{
    /// <summary>
    /// Имя модуля типа, модуль должен быть уникален в контексте компиляции.
    /// </summary>
    public string ModuleName { get; } 
}