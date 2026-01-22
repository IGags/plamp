using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Alternative.Visitors.ModulePreCreation;

namespace plamp.Alternative.Visitors;

public class BodyLevelExpressionValidator : BaseValidator<PreCreationContext, PreCreationContext>
{
    protected override VisitResult PreVisitInstruction(NodeBase node, PreCreationContext context, NodeBase? parent)
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

        return VisitResult.SkipChildren;
    }

    protected override PreCreationContext CreateInnerContext(PreCreationContext context) => context;

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, PreCreationContext innerContext) => innerContext;
}