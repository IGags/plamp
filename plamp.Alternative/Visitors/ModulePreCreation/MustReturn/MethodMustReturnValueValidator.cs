using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MustReturn;

public class MethodMustReturnValueValidator : BaseValidator<PreCreationContext, MustReturnValueInnerContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, MustReturnValueInnerContext context, NodeBase? parent)
    {
        if (node.ReturnType is { } typeNode && typeNode.Symbol == typeof(void)) return VisitResult.SkipChildren;
        
        //Root body lexical scope
        context.LexicalScopeAlwaysReturns = false;
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitFunction(FuncNode node, MustReturnValueInnerContext context, NodeBase? parent)
    {
        if (node.ReturnType is { } typeNode && typeNode.Symbol == typeof(void)) return VisitResult.SkipChildren;
        if (context.LexicalScopeAlwaysReturns) return VisitResult.SkipChildren;
        SetExceptionToSymbol(node, PlampExceptionInfo.FuncMustReturnValue(), context);
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitCondition(ConditionNode node, MustReturnValueInnerContext context, NodeBase? parent)
    {
        if (node.ElseClause == null)
        {
            context.LexicalScopeAlwaysReturns = false;
            return VisitResult.SkipChildren;
        }
        
        context.LexicalScopeAlwaysReturns = false;
        VisitChildren(node.IfClause, context);
        var ifReturns = context.LexicalScopeAlwaysReturns;
        if (!ifReturns) return VisitResult.SkipChildren;
        
        context.LexicalScopeAlwaysReturns = false;
        VisitChildren(node.ElseClause, context);
        var elseReturns = context.LexicalScopeAlwaysReturns;
        
        context.LexicalScopeAlwaysReturns = ifReturns && elseReturns;
        return VisitResult.SkipChildren;
    }

    //Body of cycle can be not executed if predicate is false, otherwise we think that cycles is false
    protected override VisitResult PreVisitWhile(WhileNode node, MustReturnValueInnerContext context, NodeBase? parent) 
        => VisitResult.SkipChildren;

    protected override VisitResult PreVisitReturn(ReturnNode node, MustReturnValueInnerContext context, NodeBase? parent)
    {
        context.LexicalScopeAlwaysReturns = true;
        return VisitResult.SkipChildren;
    }

    protected override MustReturnValueInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, MustReturnValueInnerContext innerContext) => outerContext;
}