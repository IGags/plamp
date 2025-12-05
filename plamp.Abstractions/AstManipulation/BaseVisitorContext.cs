using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Abstractions.AstManipulation;

/// <summary>
/// Базовый тип контекста, который гарантирует, что посетитель получит некоторые необходимые поля.
/// </summary>
/// <param name="translationTable">Таблица символов, в которой находятся узлы текущего AST.</param>
public abstract class BaseVisitorContext(ITranslationTable translationTable)
{
    /// <summary>
    /// Таблица символов, в которой находятся узлы текущего AST.
    /// </summary>
    public ITranslationTable TranslationTable { get; init; } = translationTable;

    /// <summary>
    /// Список ошибок, в который попадают ошибки при обходе.
    /// </summary>
    public List<PlampException> Exceptions { get; init; } = [];

    /// <summary>
    /// Список зависимостей текущего компилируемого модуля.
    /// </summary>
    //TODO: Сделать безопаснее.
    public List<ISymbolTable> Dependencies { get; init; } = [];

    protected BaseVisitorContext(BaseVisitorContext other) : this(other.TranslationTable)
    {
        Exceptions = other.Exceptions;
        Dependencies = other.Dependencies;
    }
}