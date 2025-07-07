using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Abstractions.AstManipulation.Modification;

public interface IWeaver<in TContext, out TResult>
{
    public TResult WeaveDiffs(NodeBase ast, TContext context);
}