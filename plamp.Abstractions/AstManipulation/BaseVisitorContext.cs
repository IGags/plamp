using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Func;

namespace plamp.Abstractions.AstManipulation;

/// <summary>
/// Базовый тип контекста, который гарантирует, что посетитель получит некоторые необходимые поля.
/// </summary>
/// <param name="fileName">Имя исходного файла, для которого работает посетитель</param>
/// <param name="symbolTable">Таблица символов, в которой находятся узлы текущего AST.</param>
public abstract class BaseVisitorContext(string fileName, ISymbolTable symbolTable)
{
    /// <summary>
    /// Имя исходного файла, для которого работает посетитель
    /// </summary>
    //TODO: ответственность таблицы символов. Перенести туда.
    public string FileName { get; init; } = fileName;

    /// <summary>
    /// Имя кодового модуля, для которого происходит обход.
    /// </summary>
    //TODO: ответственность таблицы символов. Перенести туда.
    public string? ModuleName { get; set; }
    
    /// <summary>
    /// Таблица символов, в которой находятся узлы текущего AST.
    /// </summary>
    public ISymbolTable SymbolTable { get; init; } = symbolTable;

    /// <summary>
    /// Функции, объявленные в кодовом файле.
    /// </summary>
    //TODO: ответственность таблицы символов. Перенести туда.
    public Dictionary<string, FuncNode> Functions { get; init; } = [];

    /// <summary>
    /// Список ошибок, в который попадают ошибки при обходе.
    /// </summary>
    public List<PlampException> Exceptions { get; init; } = [];

    protected BaseVisitorContext(BaseVisitorContext other) : this(other.FileName, other.SymbolTable)
    {
        ModuleName = other.ModuleName;
        Exceptions = other.Exceptions;
        Functions = other.Functions;
    }
}