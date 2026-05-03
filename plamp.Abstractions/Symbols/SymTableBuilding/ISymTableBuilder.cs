using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Объект, в контексте компиляции текущего модуля собирающий информацию об объявленных в модуле членах.
/// В дальнейшем информация из этого объекта транслируется в сборку.
/// Интерфейс специально отделён от таблицы символов, чтобы показать, что после компиляции модуля она переходит в таблицу символов.
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
    public ITypeBuilderInfo DefineType(TypedefNode typeNode, IGenericParameterBuilder[]? generics = null);
    
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
    /// <param name="generics">Список параметров у функции. Пустой массив или null расцениваются так, что функция не является дженериком.</param>
    /// <returns>Базовый объект - билдер объявленной функции</returns>
    public IFnBuilderInfo DefineFunc(FuncNode fnNode, IGenericParameterBuilder[]? generics = null);

    /// <summary>
    /// Создаёт ни к чему(к таблице символов) не привязанный дженерик параметр
    /// </summary>
    /// <param name="genericNode">Узел из которого требуется создать параметр</param>
    /// <returns>Информация о типе дженерик параметра</returns>
    public IGenericParameterBuilder CreateGenericParameter(GenericDefinitionNode genericNode);

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
    /// Получить информацию о типе по его имени.
    /// </summary>
    /// <param name="name">Имя типа</param>
    /// <param name="typeInfo">Информация о типе в контексте компиляции текущего модуля</param>
    /// <returns>Флаг успеха операции</returns>
    public bool TryGetInfo(string name, [NotNullWhen(true)] out ITypeBuilderInfo? typeInfo);

    /// <summary>
    /// Получить информацию о функции по её имени.
    /// </summary>
    /// <param name="name">Имя функции</param>
    /// <param name="fnInfo">Информациц о функции в контексте компиляции текущего модуля.</param>
    /// <returns>Флаг успеха операции</returns>
    public bool TryGetInfo(string name, [NotNullWhen(true)] out IFnBuilderInfo? fnInfo);

    /// <summary>
    /// Попытаться получить узел ast объявления функции внутри компилируемого модуля.
    /// </summary>
    /// <param name="info">Информация о функции во время компиляции</param>
    /// <param name="defNode">В случае, если модуль действительно содержит такую функцию - будет получено её объявление</param>
    /// <returns>Флаг успеха поиска</returns>
    public bool TryGetDefinition(IFnBuilderInfo info, [NotNullWhen(true)]out FuncNode? defNode);
}