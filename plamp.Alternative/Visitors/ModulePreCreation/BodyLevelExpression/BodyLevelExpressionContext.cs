using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.Visitors.ModulePreCreation.BodyLevelExpression;

public class BodyLevelExpressionContext : PreCreationContext
{
    public List<NodeBase> ToRemove { get; } = [];
    
    public BodyLevelExpressionContext(PreCreationContext other) : base(other)
    {
    }
}