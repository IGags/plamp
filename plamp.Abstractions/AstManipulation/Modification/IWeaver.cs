using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Modification;

public interface IWeaver<TContext> where TContext : BaseVisitorContext
{
    public TContext WeaveDiffs(NodeBase ast, TContext context);
}