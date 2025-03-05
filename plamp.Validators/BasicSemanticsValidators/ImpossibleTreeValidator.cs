using System.Collections.Generic;
using System.Security.Cryptography;
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
                case CastNode:
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
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingForeachBody();
            SetExceptionToNode(exceptionRecord, node);
        }

        if (node.Iterator == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ForeachIteratorVarMustExist();
            SetExceptionToNode(exceptionRecord, node);
        }
        
        switch (node.Iterator)
        {
            case null:
            case VariableDefinitionNode:
                break;
            default:
                var exceptionRecord = PlampSemanticsExceptions.ForeachIteratorMustBeVariableDeclaration();
                SetExceptionToNode(exceptionRecord, node.Iterator);
                break;
        }

        if (node.Iterable == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ForeachIterableMustExist();
            SetExceptionToNode(exceptionRecord, node);
        }

        switch (node.Iterable)
        {
            case null:
            case MemberNode:
            case MemberAccessNode:
            case CallNode:
            case IndexerNode:
            case ConstructorNode:
            case CastNode:
                break;
            default:
                var exceptionRecord = PlampSemanticsExceptions.MismatchForeachIterableNodeType();
                SetExceptionToNode(exceptionRecord, node.Iterable);
                break;
        }
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitWhile(WhileNode node)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingWhileBody();
            SetExceptionToNode(exceptionRecord, node);
        }

        if (node.Condition == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingWhileCondition();
            SetExceptionToNode(exceptionRecord, node);
            return VisitResult.Continue;
        }
        
        switch (node.Condition)
        {
            case null:
            case MemberNode:
            case MemberAccessNode:
            case CallNode:
            case IndexerNode:
            case LiteralNode:
            case BaseUnaryNode:
            case CastNode:
            case BaseBinaryNode:
                if(node.Condition is BaseAssignNode) goto default;
                return VisitResult.Continue;
            default:
                var exceptionRecord = PlampSemanticsExceptions.MismatchWhileConditionNodeType();
                SetExceptionToNode(exceptionRecord, node.Condition);
                return VisitResult.Continue;
        }
    }

    protected override VisitResult VisitCondition(ConditionNode node)
    {
        if (node.IfClause != null) return VisitResult.Continue;
        var exceptionRecord = PlampSemanticsExceptions.ConditionMustHaveBaseClause();
        SetExceptionToNode(exceptionRecord, node);

        return VisitResult.Continue;
    }

    protected override VisitResult VisitClause(ClauseNode node)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ConditionClauseMustHaveBody();
            SetExceptionToNode(exceptionRecord, node);
        }

        switch (node.Predicate)
        {
            case null:
                var exceptionRecord = PlampSemanticsExceptions.ClausePredicateMustExist();
                SetExceptionToNode(exceptionRecord, node);
                return VisitResult.Continue;
            case MemberNode:
            case MemberAccessNode:
            case CallNode:
            case IndexerNode:
            case LiteralNode:
            case BaseUnaryNode:
            case CastNode:
            case BaseBinaryNode:
                if(node.Predicate is BaseAssignNode) goto default;
                return VisitResult.Continue;
            default:
                exceptionRecord = PlampSemanticsExceptions.ConditionClausePredicateNodeTypeMismatch();
                SetExceptionToNode(exceptionRecord, node.Predicate);
                return VisitResult.Continue;
        }
    }

    protected override VisitResult VisitBreak(BreakNode node)
    {
        return VisitResult.Continue;
    }

    protected override VisitResult VisitReturn(ReturnNode node)
    {
        //Return can be made from a void method
        return VisitResult.Continue;
    }

    protected override VisitResult VisitContinue(ContinueNode node)
    {
        return VisitResult.Continue;
    }

    #endregion

    #region Unary

    protected override VisitResult VisitUnaryNode(BaseUnaryNode unaryNode)
    {
        if (unaryNode.Inner == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.UnaryNodeRequiresUnderlyingNode();
            SetExceptionToNode(exceptionRecord, unaryNode);
        }
        
        switch (unaryNode.Inner)
        {
            case MemberNode:
            case MemberAccessNode:
            case CallNode:
            case LiteralNode:
            case IndexerNode:
            case BaseBinaryNode:
            case CastNode:
                return VisitResult.Continue;
            default:
                var exceptionRecord = PlampSemanticsExceptions.UnaryNodeInnerNodeTypeMismatch();
                SetExceptionToNode(exceptionRecord, unaryNode);
                return VisitResult.Continue;
        }
    }

    #endregion

    #region Object members(eg. call, indexer, prop)

    protected override VisitResult VisitCall(CallNode node)
    {
        ValidateFromMemberInMemberInteraction(node, node.From, 
            PlampSemanticsExceptions.CallNodeRequireCallerNode(),
            PlampSemanticsExceptions.CallerNodeTypeMismatch());

        ValidateArgsInMemberInteraction(node, node.Args,
            PlampSemanticsExceptions.ArgNodeMustNotBeNull(),
            PlampSemanticsExceptions.ArgMustBeValueReturningNode());
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitIndexer(IndexerNode node)
    {
        ValidateFromMemberInMemberInteraction(node, node.ToIndex, 
            PlampSemanticsExceptions.IndexNodeRequireIndexableNode(),
            PlampSemanticsExceptions.IndexableNodeTypeMismatch());

        ValidateArgsInMemberInteraction(node, node.Arguments,
            PlampSemanticsExceptions.ArgNodeMustNotBeNull(),
            PlampSemanticsExceptions.IndexerArgMustBeReturningNode());
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitMemberAccess(MemberAccessNode accessNode)
    {
        if (accessNode.From == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.CannotAccessMemberFromNothing();
            SetExceptionToNode(exceptionRecord, accessNode);
        }

        if (accessNode.Member == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.CannotAccessNothing();
            SetExceptionToNode(exceptionRecord, accessNode);
        }

        switch (accessNode.From)
        {
            case null:
            case CallNode:
            case MemberNode:
            case IndexerNode:
            case LiteralNode:
            case CastNode:
            case BaseUnaryNode:
            case BaseBinaryNode:
                if(accessNode.From is BaseAssignNode) goto default;
                break;
            default:
                var exceptionRecord = PlampSemanticsExceptions.AccessibleNodeTypeMismatch();
                SetExceptionToNode(exceptionRecord, accessNode.From);
                break;
        }

        if (accessNode.Member is not MemberNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.MemberAccessTargetMustBeMember();
            SetExceptionToNode(exceptionRecord, accessNode.Member);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult VisitConstructor(ConstructorNode node)
    {
        
    }

    protected override VisitResult VisitCast(CastNode node)
    {
        return base.VisitCast(node);
    }

    #endregion

    #region Global

    protected override VisitResult VisitDef(DefNode node)
    {
        return base.VisitDef(node);
    }

    protected override VisitResult VisitUse(UseNode node)
    {
        return base.VisitUse(node);
    }

    #endregion

    #region Misc(arg, type)

    protected override VisitResult VisitType(TypeNode node)
    {
        return base.VisitType(node);
    }

    protected override VisitResult VisitParameter(ParameterNode node)
    {
        return base.VisitParameter(node);
    }

    protected override VisitResult VisitVariableDefinition(VariableDefinitionNode node)
    {
        return base.VisitVariableDefinition(node);
    }

    protected override VisitResult VisitMember(MemberNode node)
    {
        return base.VisitMember(node);
    }

    protected override VisitResult VisitLiteral(LiteralNode literalNode)
    {
        return base.VisitLiteral(literalNode);
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

    private void ValidateFromMemberInMemberInteraction(NodeBase parent, NodeBase from, 
        PlampExceptionRecord nullException, PlampExceptionRecord typeMismatch)
    {
        if (from == null)
        {
            SetExceptionToNode(nullException, parent);
        }

        switch (from)
        {
            case null:
            case MemberNode:
            case MemberAccessNode:
            case IndexerNode:
            case BaseBinaryNode:
            case CastNode:
            case BaseUnaryNode:
                if(from is BaseAssignNode) goto default;
                break;
            default:
                SetExceptionToNode(typeMismatch, from);
                break;
        }
    }

    private void ValidateArgsInMemberInteraction(NodeBase parent, List<NodeBase> args, 
        PlampExceptionRecord possibleArgNull, PlampExceptionRecord typeMismatch)
    {
        var hasNullArg = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case null:
                    hasNullArg = true;
                    continue;
                case MemberNode:
                case MemberAccessNode:
                case CallNode:
                case LiteralNode:
                case IndexerNode:
                case BaseBinaryNode:
                case CastNode:
                case BaseUnaryNode:
                    if(arg is BaseAssignNode) goto default;
                    continue;
                default:
                    SetExceptionToNode(typeMismatch, arg);
                    continue;
            }
        }

        if (!hasNullArg) return;
        SetExceptionToNode(possibleArgNull, parent);
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