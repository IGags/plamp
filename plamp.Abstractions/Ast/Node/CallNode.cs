using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий операцию вызова фукции
/// </summary>
/// <param name="from">Объект, чью функцию требуется вызвать. Отсутствие значения означает статическую функцию.</param>
/// <param name="name">Имя вызываемой функции</param>
/// <param name="args">Список аргументов, который передаётся функции</param>
public class CallNode(NodeBase? from, FuncCallNameNode name, List<NodeBase> args, List<TypeNode> genericArguments) : NodeBase
{
    /// <summary>
    /// Объект, чью функцию требуется вызвать. Отсутствие значения означает статическую функцию.
    /// </summary>
    public NodeBase? From { get; private set; } = from;

    /// <summary>
    /// Имя вызываемой функции
    /// </summary>
    public FuncCallNameNode Name { get; private set; } = name;
    
    /// <summary>
    /// Список аргументов, который передаётся функции
    /// </summary>
    public IReadOnlyList<NodeBase> Args => args;

    /// <summary>
    /// Список типов дженерик аргументов с которыми вызвана функция. Появляется только при явной их передаче.
    /// В остальных случаях [].
    /// </summary>
    public IReadOnlyList<TypeNode> GenericArguments => genericArguments;

    public IFnInfo? FnInfo { get; set; }

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        if(From != null) yield return From;

        foreach (var arg in genericArguments)
        {
            yield return arg;
        }
        
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int argIndex;
        if (From == child)
        {
            From = newChild;
        }
        else if (Name == child && newChild is FuncCallNameNode member)
        {
            Name = member;
        }
        else if(child is TypeNode oldGeneric 
                && newChild is TypeNode newGeneric 
                && -1 != (argIndex = genericArguments.IndexOf(oldGeneric)))
        {
            genericArguments[argIndex] = newGeneric;
        }
        else if (-1 != (argIndex = args.IndexOf(child)))
        {
            args[argIndex] = newChild;
        }
    }
}