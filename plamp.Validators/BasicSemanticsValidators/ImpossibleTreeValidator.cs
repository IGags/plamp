using System.Collections.Generic;
using plamp.Ast;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Unary;
using plamp.Validators.Abstractions;
using plamp.Validators.Models;

namespace plamp.Validators.BasicSemanticsValidators;

public class ImpossibleTreeValidator : BaseValidator
{
    private readonly ValidationResult _validationResult = new();

    private ValidationContext _context;

    //Nodes to skip, tree enumeration logic isn't enough
    private HashSet<NodeBase> _skippedBranches = [];
    
    //IDK need reuse or no need => do single usable
    //Do not thread safe
    public override ValidationResult Validate(ValidationContext context)
    {
        _skippedBranches.Clear();
        _context = context;
        VisitInternal(context.Ast);
        return _validationResult;
    }

    #region Assign

    protected override VisitResult VisitBaseAssign(BaseAssignNode node)
    {
        if (node is AssignNode assignNode) return VisitAssign(assignNode);
        _ = ValidateBinaryBranch(node.Right);

        switch (node.Left)
        {
            case MemberNode:
            case MemberAccessNode:
                return VisitResult.Continue;
        }

        var exceptionRecord = PlampSemanticsExceptions.InvalidChangeAndReAssignmentTarget();
        SetExceptionToNodeAndChildren(exceptionRecord, node.Left);
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitAssign(AssignNode node)
    {
        _ = ValidateBinaryBranch(node.Right);

        switch (node.Left)
        {
            case MemberNode:
            case MemberAccessNode:
            case VariableDefinitionNode:
                return base.VisitAssign(node);
        }
        
        var exceptionRecord = PlampSemanticsExceptions.InvalidAssignmentTarget();
        SetExceptionToNodeAndChildren(exceptionRecord, node.Left);
        return base.VisitAssign(node);
    }

    #endregion

    #region Shared rules

    private VisitResult ValidateBinaryBranch(NodeBase leaf)
    {
        switch (leaf)
        {
            case BaseBinaryNode bin:
                if(bin is BaseAssignNode) break;
                return VisitResult.Continue;
            case CallNode:
            case ConstructorNode:
            case IndexerNode:
            case MemberAccessNode:
            case CastNode:
            case BaseUnaryNode:
            case LiteralNode:
            case MemberNode:
                return VisitResult.Continue;
        }

        var exceptionRecord = PlampSemanticsExceptions.InvalidChildExpression();
        SetExceptionToNodeAndChildren(exceptionRecord, leaf);
        
        return VisitResult.Continue;
    }

    #endregion

    #region Helper logic

    private void SetExceptionToNodeAndChildren(PlampExceptionRecord exceptionRecord, NodeBase node)
    {
        var exception = _context.Table.SetExceptionToNodeAndChildren(exceptionRecord, node);
        _validationResult.Exceptions.Add(exception);
        _skippedBranches.Add(node);
    }

    #endregion
}