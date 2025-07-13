using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MustReturn;

public class MethodMustReturnValueValidator : BaseValidator<PreCreationContext, MustReturnValueInnerContext>
{
    protected override VisitResult VisitDef(DefNode node, MustReturnValueInnerContext context)
    {
        if (node.ReturnType is { } typeNode && typeNode.Symbol == typeof(void)) return VisitResult.SkipChildren;
        
        //Root body lexical scope
        context.LexicalScopeAlwaysReturns = false;
        VisitChildren(node, context);
        if (context.LexicalScopeAlwaysReturns) return VisitResult.SkipChildren;
        
        var exception =
            context.SymbolTable.SetExceptionToNode(node, PlampExceptionInfo.FuncMustReturnValue(), context.FileName);
        context.Exceptions.Add(exception);

        return VisitResult.SkipChildren;
    }

    //TODO: think about easier solution
    protected override VisitResult VisitCondition(ConditionNode node, MustReturnValueInnerContext context)
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
    protected override VisitResult VisitWhile(WhileNode node, MustReturnValueInnerContext context) 
        => VisitResult.SkipChildren;

    protected override VisitResult VisitReturn(ReturnNode node, MustReturnValueInnerContext context)
    {
        context.LexicalScopeAlwaysReturns = true;
        return VisitResult.SkipChildren;
    }

    protected override MustReturnValueInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, MustReturnValueInnerContext innerContext) => outerContext;
}