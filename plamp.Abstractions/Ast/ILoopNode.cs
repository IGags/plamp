using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast;

public interface ILoopNode
{
    NodeBase Body { get; }
}