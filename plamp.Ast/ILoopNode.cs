using plamp.Ast.Node; 

namespace plamp.Ast;

public interface ILoopNode
{
    NodeBase Body { get; }
}