using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Alternative.Visitors.ModulePreCreation.BodyLevelExpression;

namespace plamp.Alternative.Visitors.ModulePreCreation;

public class BodyLevelExpressionValidator : BaseWeaver<PreCreationContext, BodyLevelExpressionContext>
{
    protected override VisitResult PreVisitInstruction(NodeBase node, BodyLevelExpressionContext context, NodeBase? parent)
    {
        if (node
            is ReturnNode
            or CallNode
            or AssignNode
            or PrefixDecrementNode
            or PrefixIncrementNode
            or PostfixDecrementNode
            or PostfixDecrementNode
            or ConditionNode
            or WhileNode
            or ContinueNode
            or BreakNode
            or EmptyNode
            or VariableDefinitionNode)
        {
            return VisitResult.SkipChildren;
        }
        
        var error = PlampExceptionInfo.IllegalBodyLevelInstruction();
        SetExceptionToSymbol(node, error, context);
        
        context.ToRemove.Add(node);

        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitBody(BodyNode node, BodyLevelExpressionContext context, NodeBase? parent)
    {
        if (parent == null) return VisitResult.SkipChildren;
        var newInstructions = node.ExpressionList.Except(context.ToRemove).ToList();
        var newBody = new BodyNode(newInstructions);
        Replace(node, _ => newBody, context);
        return VisitResult.SkipChildren;
    }

    protected override BodyLevelExpressionContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(BodyLevelExpressionContext innerContext, PreCreationContext outerContext) => outerContext;
}