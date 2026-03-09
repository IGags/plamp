using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Объект, в контексте компиляции текущего модуля собирающий информацию об объявленных в модуле членах.
/// В дальнейшем информация из этого объекта транслируется в сборку.
/// </summary>
public interface ISymTableBuilder
{
    /// <summary>
    /// Имя компилируемого модуля, если не определено в модуле - подставляется значение по умолчанию в угловых скобках
    /// </summary>
    public string ModuleName { get; set; }

    /// <summary>
    /// Объявить новый тип в компилируемом модуле.
    /// Валидация на дубликаты по чему-либо не гарантируется
    /// </summary>
    /// <param name="typeNode">Узел AST объявляющий тип</param>
    /// <param name="generics">Список параметров у типа. Пустрой массив или null расцениваются так, что тип не является дженериком.</param>
    /// <returns>Базовый объект - билдер объявленного типа</returns>
    public ITypeBuilderInfo DefineType(TypedefNode typeNode, GenericDefinitionNode[]? generics = null);
    
    /// <summary>
    /// Перечислить все типы, явно объявленные через вызов <see cref="DefineType"/> в текущем билдере 
    /// </summary>
    /// <returns>Список явно объявленных типов</returns>
    public IReadOnlyList<ITypeBuilderInfo> ListTypes();

    /// <summary>
    /// Объявить новую функцию в компилируемом модуле.
    /// Валидация на дубликаты по чему-либо не гарантируется
    /// </summary>
    /// <param name="fnNode">Узел AST объявляющий функцию</param>
    /// <returns>Базовый объект - билдер объявленной функции</returns>
    public IFnBuilderInfo DefineFunc(FuncNode fnNode);

    /// <summary>
    /// Перечислить все функции, явно объявленные через вызов <see cref="DefineFunc"/> в текущем билдере
    /// </summary>
    /// <returns>Список явно объявленных функций</returns>
    public IReadOnlyList<IFnBuilderInfo> ListFuncs();
}