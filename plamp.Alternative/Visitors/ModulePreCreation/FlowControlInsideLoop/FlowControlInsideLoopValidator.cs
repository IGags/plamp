using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.FlowControlInsideLoop;

public class FlowControlInsideLoopValidator : BaseValidator<PreCreationContext, FlowControlInsideLoopContext>
{
    protected override FlowControlInsideLoopContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, FlowControlInsideLoopContext innerContext) => innerContext;

    protected override VisitResult PreVisitWhile(WhileNode node, FlowControlInsideLoopContext context, NodeBase? parent)
    {
        context.LoopDepth++;
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitWhile(WhileNode node, FlowControlInsideLoopContext context, NodeBase? parent)
    {
        context.LoopDepth--;
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitBreak(BreakNode node, FlowControlInsideLoopContext context, NodeBase? parent)
    {
        if (context.LoopDepth != 0) return VisitResult.SkipChildren;
        var record = PlampExceptionInfo.CannotUseControlFlowNotInLoop();
        SetExceptionToSymbol(node, record, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitContinue(ContinueNode node, FlowControlInsideLoopContext context, NodeBase? parent)
    {
        if (context.LoopDepth != 0) return VisitResult.SkipChildren;
        var record = PlampExceptionInfo.CannotUseControlFlowNotInLoop();
        SetExceptionToSymbol(node, record, context);
        return VisitResult.SkipChildren;
    }
}