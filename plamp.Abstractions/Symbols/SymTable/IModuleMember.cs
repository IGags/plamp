namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Общий интерфейс для всех объявлений верхнего уровня внутри модуля.
/// </summary>
public interface IModuleMember
{
    /// <summary>
    /// Имя модуля типа, модуль должен быть уникален в контексте компиляции.
    /// Реализации, принимающие имя модуля при создании объекта, должны бросать <see cref="System.InvalidOperationException"/>,
    /// если имя модуля пустое или состоит только из пробельных символов.
    /// </summary>
    public string ModuleName { get; } 
}
