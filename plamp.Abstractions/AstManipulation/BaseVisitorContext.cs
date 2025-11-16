using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Abstractions.AstManipulation;

/// <summary>
/// Базовый тип контекста, который гарантирует, что посетитель получит некоторые необходимые поля.
/// </summary>
/// <param name="translationTable">Таблица символов, в которой находятся узлы текущего AST.</param>
public abstract class BaseVisitorContext(ITranslationTable translationTable)
{
    /// <summary>
    /// Имя кодового модуля, для которого происходит обход.
    /// </summary>
    //TODO: ответственность таблицы символов. Перенести туда.
    public string? ModuleName { get; set; }
    
    /// <summary>
    /// Таблица символов, в которой находятся узлы текущего AST.
    /// </summary>
    public ITranslationTable TranslationTable { get; init; } = translationTable;

    /// <summary>
    /// Функции, объявленные в кодовом файле.
    /// </summary>
    //TODO: ответственность таблицы символов. Перенести туда.
    public Dictionary<string, FuncNode> Functions { get; init; } = [];

    /// <summary>
    /// Типы, объявленные в кодовом файле.
    /// </summary>
    //TODO: ответственность таблицы символов. Перенести туда.
    public Dictionary<string, TypedefNode> Types { get; init; } = [];

    /// <summary>
    /// Список ошибок, в который попадают ошибки при обходе.
    /// </summary>
    public List<PlampException> Exceptions { get; init; } = [];

    protected BaseVisitorContext(BaseVisitorContext other) : this(other.TranslationTable)
    {
        ModuleName = other.ModuleName;
        Exceptions = other.Exceptions;
        Functions = other.Functions;
    }
}