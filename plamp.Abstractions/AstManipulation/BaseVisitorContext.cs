using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.AstManipulation;

/// <summary>
/// Базовый тип контекста, который гарантирует, что посетитель получит некоторые необходимые поля.
/// </summary>
/// <param name="translationTable">Таблица символов, в которой находятся узлы текущего AST.</param>
public abstract class BaseVisitorContext(
    ITranslationTable translationTable,
    IReadOnlyList<ISymTable> dependencies)
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
    /// Полный список явных зависимостей текущего компилируемого модуля. Включая сам модуль.
    /// </summary>
    public IReadOnlyList<ISymTable> Dependencies { get; init; } = dependencies;

    protected BaseVisitorContext(BaseVisitorContext other) 
        : this(other.TranslationTable, other.Dependencies)
    {
        Exceptions = other.Exceptions;
    }
}