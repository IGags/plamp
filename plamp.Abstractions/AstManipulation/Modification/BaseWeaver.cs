using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Abstractions.AstManipulation.Modification;

public abstract class BaseWeaver<TContext> : BaseVisitor<TContext>, IWeaver<TContext>
{
    public abstract WeaveResult WeaveDiffs(NodeBase ast, TContext context);
}