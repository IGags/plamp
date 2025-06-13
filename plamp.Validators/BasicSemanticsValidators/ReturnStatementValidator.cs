using System.Threading;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Validation;
using plamp.Abstractions.Validation.Models;

namespace plamp.Validators.BasicSemanticsValidators;

public class ReturnStatementValidator : BaseValidator<ValidationContext>
{
    public override ValidationResult Validate(ValidationContext context, CancellationToken cancellationToken)
    {
        VisitInternal(context.Ast, context);
        return new ValidationResult() { Exceptions = context.Exceptions };
    }

    protected override VisitResult VisitDef(DefNode node, ValidationContext context)
    {
        if (node.Body == null) return VisitResult.SkipChildren;
        if (VisitBody(node.Body, false)) return VisitResult.SkipChildren;
        
        var exceptionRecord = PlampSemanticsExceptions.DefMustReturnValue();
        context.Table.SetExceptionToNodeWithoutChildren(exceptionRecord, node, context.FileName, context.AssemblyName);
        return VisitResult.SkipChildren;
    }

    /// <summary>
    /// Body should return value in every path to be full return
    /// </summary>
    private bool VisitBody(BodyNode node, bool isInCycleContext)
    {
        var returnsFully = true;
        foreach (var instruction in node.InstructionList)
        {
            switch (instruction)
            {
                case ReturnNode:
                    return true;
                case ContinueNode:
                case BreakNode:
                    if(isInCycleContext) return returnsFully;
                    break;
                case BodyNode body:
                    returnsFully &= VisitBody(body, isInCycleContext);
                    break;
                case ConditionNode cond:
                    returnsFully &= VisitCondition(cond, isInCycleContext, returnsFully);
                    break;
                case ILoopNode loopNode:
                    if (loopNode.Body is BodyNode forBody)
                        returnsFully &= VisitBody(forBody, true);
                    else returnsFully = false;
                    break;
            }
        }

        return returnsFully;
    }

    private bool VisitCondition(ConditionNode cond, bool isInCycleContext, bool returnsFully)
    {
        // foreach (var elifClause in cond.ElifClauseList)
        // {
        //     var clauseBody = elifClause.Body as BodyNode;
        //     if (clauseBody == null)
        //     {
        //         returnsFully = false;
        //         continue;
        //     }
        //     returnsFully &= VisitBody(clauseBody, isInCycleContext);
        // }
        // var ifClauseBody = cond.IfClause.Body as BodyNode;
        //             
        // returnsFully &= ifClauseBody != null && VisitBody(ifClauseBody, isInCycleContext); 
        // if(cond.ElseClause != null) returnsFully &= VisitBody(cond.ElseClause, isInCycleContext);
        //
        // return returnsFully;
        return false;
    }
}