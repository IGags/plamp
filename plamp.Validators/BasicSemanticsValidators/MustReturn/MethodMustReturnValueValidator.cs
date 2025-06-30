using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Abstractions.AstManipulation.Validation.Models;

namespace plamp.Validators.BasicSemanticsValidators.MustReturn;

public class MethodMustReturnValueValidator : BaseValidator<MustReturnValueContext, MustReturnValueInnerContext>
{
    protected override MustReturnValueInnerContext MapContext(
        MustReturnValueContext context)
    {
        var innerContext = new MustReturnValueInnerContext
        {
            Exceptions = context.Exceptions,
            SymbolTable = context.SymbolTable
        };
        //Root body lexical scope
        innerContext.LexicalScopeAlwaysReturns.Push(false);
        return innerContext;
    }

    protected override ValidationResult CreateResult(
        MustReturnValueContext outerContext, 
        MustReturnValueInnerContext innerContext)
    {
        if (!innerContext.LexicalScopeAlwaysReturns.Pop())
        {
            var exceptions
        }
        return new ValidationResult { Exceptions = innerContext.Exceptions };
    }

    //TODO: think about easier solution
    protected override VisitResult VisitCondition(ConditionNode node, MustReturnValueInnerContext context)
    {
        if (node.ElseClause == null)
        {
            context.LexicalScopeAlwaysReturns.Pop();
            context.LexicalScopeAlwaysReturns.Push(false);
            return VisitResult.SkipChildren;
        }
        
        context.LexicalScopeAlwaysReturns.Push(false);
        VisitChildren(node.IfClause.Visit(), context);
        var ifReturns = context.LexicalScopeAlwaysReturns.Pop();
        
        context.LexicalScopeAlwaysReturns.Push(false);
        VisitChildren(node.ElseClause.Visit(), context);
        var elseReturns = context.LexicalScopeAlwaysReturns.Pop();
        
        context.LexicalScopeAlwaysReturns.Push(ifReturns && elseReturns);
        
        return VisitResult.SkipChildren;
    }

    //Body of cycle can be not executed if predicate is false, otherwise we think that cycles is false
    protected override VisitResult VisitWhile(WhileNode node, MustReturnValueInnerContext context) 
        => VisitResult.SkipChildren;

    protected override VisitResult VisitReturn(ReturnNode node, MustReturnValueInnerContext context)
    {
        context.LexicalScopeAlwaysReturns.Pop();
        context.LexicalScopeAlwaysReturns.Push(true);
        return VisitResult.SkipChildren;
    }
}