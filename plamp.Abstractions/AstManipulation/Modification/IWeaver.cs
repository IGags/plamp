using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Abstractions.AstManipulation.Modification;

public interface IWeaver<in TContext>
{
    public WeaveResult WeaveDiffs(NodeBase ast, TContext context);
}