using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.BodyLevelExpression;

/// <summary>
/// Валидация разрешённых выражений на уровне body.
/// </summary>
public class BodyLevelExpressionValidator : BaseValidator<PreCreationContext, BodyLevelExpressionContext>
{
    /// <inheritdoc/>
    protected override VisitorGuard Guard => VisitorGuard.FuncDefWithBody;

    /// <inheritdoc/>
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

        return VisitResult.SkipChildren;
    }

    /// <inheritdoc/>
    protected override BodyLevelExpressionContext CreateInnerContext(PreCreationContext context) => new(context);
    
    /// <inheritdoc/>
    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, BodyLevelExpressionContext innerContext) => outerContext;
}