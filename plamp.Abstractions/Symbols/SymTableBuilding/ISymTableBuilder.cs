using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// <param name="generics">Список параметров у типа. Пустой массив или null расцениваются так, что тип не является дженериком.</param>
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

    /// <summary>
    /// Попытаться получить узел ast объявления типа внутри компилируемого модуля.
    /// </summary>
    /// <param name="info">Информация о типе во время компиляции</param>
    /// <param name="defNode">В случае, если модуль действительно содержит такой тип - будет получено его объявление</param>
    /// <returns>Флаг успеха поиска</returns>
    public bool TryGetDefinition(ITypeBuilderInfo info, [NotNullWhen(true)]out TypedefNode? defNode);

    /// <summary>
    /// Получить информацию о типе по его объявлению.
    /// </summary>
    /// <param name="node">Узел в ast объявления типа</param>
    /// <param name="typeInfo">Информация о типе в контексте компиляции текущего модуля</param>
    /// <returns>Флаг успеха операции</returns>
    public bool TryGetInfo(TypedefNode node, [NotNullWhen(true)] out ITypeBuilderInfo? typeInfo);

    /// <summary>
    /// Попытаться получить узел ast объявления поля внутри компилируемого модуля.
    /// </summary>
    /// <param name="info">Информация о поле во время компиляции</param>
    /// <param name="defNode">В случае, если модуль действительно содержит такое поле - будет получено его объявление</param>
    /// <returns>Флаг успеха поиска</returns>
    public bool TryGetDefinition(IFieldBuilderInfo info, [NotNullWhen(true)]out FieldDefNode? defNode);

    /// <summary>
    /// Попытаться получить узел ast объявления функции внутри компилируемого модуля.
    /// </summary>
    /// <param name="info">Информация о функции во время компиляции</param>
    /// <param name="defNode">В случае, если модуль действительно содержит такую функцию - будет получено её объявление</param>
    /// <returns>Флаг успеха поиска</returns>
    public bool TryGetDefinition(IFnBuilderInfo info, [NotNullWhen(true)]out FuncNode? defNode);
}