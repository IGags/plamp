using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Расширение интерфейса специфичное для создающегося модуля.
/// Описывает объявление функции, добавляя данные в контексте создания текущего модуля.
/// </summary>
public interface IFnBuilderInfo : IFnInfo
{
    public FuncNode Function { get; }
}