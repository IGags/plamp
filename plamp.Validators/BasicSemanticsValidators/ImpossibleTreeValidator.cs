using System.Collections.Generic;
using plamp.Ast;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
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

    protected override VisitResult VisitNodeBase(NodeBase node)
    {
        return _skippedBranches.Contains(node) 
            ? VisitResult.Continue : base.VisitNodeBase(node);
    }

    #region Assign

    protected override VisitResult VisitBaseAssign(BaseAssignNode node)
    {
        if (node is AssignNode assignNode) return VisitAssign(assignNode);
        
        if (node.Right != null)
        {
            _ = ValidateBinaryBranch(node.Right);
        }
        
        if(node.Left == null) return VisitResult.Continue;
        
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
        if (node.Right != null)
        {
            _ = ValidateBinaryBranch(node.Right);
        }

        if(node.Left == null) return VisitResult.Continue;
        
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

    #region Binary

    protected override VisitResult VisitBinaryExpression(BaseBinaryNode binaryNode)
    {
        if (binaryNode.Left == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingLeftExpression();
            SetExceptionToNode(exceptionRecord, binaryNode);
        }

        if (binaryNode.Right == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingRightExpression();
            SetExceptionToNode(exceptionRecord, binaryNode);
        }
        
        _ = ValidateBinaryBranch(binaryNode.Left);
        _ = ValidateBinaryBranch(binaryNode.Right);
        return VisitResult.Continue;
    }

    #endregion

    #region Body level

    protected override VisitResult VisitFor(ForNode node)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingForBody();
            SetExceptionToNode(exceptionRecord, node);
        }

        AddForChildException(PlampSemanticsExceptions.InvalidForLoopCounter(), node.Counter);

        if (node.IteratorVar != null && node.IteratorVar is not AssignNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.InvalidForIterator();
            SetExceptionToNode(exceptionRecord, node.IteratorVar);
        }

        AddForChildException(PlampSemanticsExceptions.InvalidForLoopCondition(), node.TilCondition);

        void AddForChildException(PlampExceptionRecord record, NodeBase child)
        {
            if (child == null) return;
            switch (child)
            {
                case BaseBinaryNode:
                case BaseUnaryNode:
                case CallNode:
                case IndexerNode:
                    break;
                default:
                    SetExceptionToNode(record, child);
                    break;
            }
        }
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitForeach(ForeachNode node)
    {
        
    }

    protected override VisitResult VisitWhile(WhileNode node)
    {
        
    }

    protected override VisitResult VisitCondition(ConditionNode node)
    {
    }

    protected override VisitResult VisitClause(ClauseNode node)
    {
        
    }

    protected override VisitResult VisitBreak(BreakNode node)
    {
        
    }

    protected override VisitResult VisitReturn(ReturnNode node)
    {
        
    }

    protected override VisitResult VisitContinue(ContinueNode node)
    {
        
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

    private void SetExceptionToNode(PlampExceptionRecord exceptionRecord, NodeBase node)
    {
        var exception = _context.Table.SetExceptionToNodeWithoutChildren(exceptionRecord, node);
        _validationResult.Exceptions.Add(exception);
        _skippedBranches.Add(node);
    }

    #endregion
}