using System.Collections.Generic;
using System.Threading;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Extensions;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Validation;
using plamp.Abstractions.Validation.Models;

namespace plamp.Validators.BasicSemanticsValidators;

public class ImpossibleTreeValidator : BaseValidator<ImpossibleTreeValidatorContext>
{
    //IDK need reuse or no need => do single usable
    //Do not thread safe
    public override ValidationResult Validate(ValidationContext context, CancellationToken cancellationToken)
    {
        var validationContext = new ImpossibleTreeValidatorContext(context)
        {
            SkippedBranches = []
        };
        VisitInternal(context.Ast, validationContext);
        return new ValidationResult() { Exceptions = validationContext.Exceptions };
    }

    protected override VisitResult VisitNodeBase(NodeBase node, ImpossibleTreeValidatorContext context)
    {
        return context.SkippedBranches.Contains(node) 
            ? VisitResult.Continue : base.VisitNodeBase(node, context);
    }

    #region Assign

    protected override VisitResult VisitBaseAssign(BaseAssignNode node, ImpossibleTreeValidatorContext context)
    {
        if (node is AssignNode assignNode) return VisitAssign(assignNode, context);
        
        if (node.Right != null)
        {
            _ = ValidateBinaryBranch(node.Right, context);
        }
        
        if(node.Left == null) return VisitResult.Continue;
        
        switch (node.Left)
        {
            case MemberNode:
            case MemberAccessNode:
                return VisitResult.Continue;
        }

        var exceptionRecord = PlampSemanticsExceptions.InvalidChangeAndReAssignmentTarget();
        SetExceptionToNodeAndChildren(exceptionRecord, node.Left, context);
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitAssign(AssignNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Right != null)
        {
            _ = ValidateBinaryBranch(node.Right, context);
        }

        if(node.Left == null) return VisitResult.Continue;
        
        switch (node.Left)
        {
            case MemberNode:
            case MemberAccessNode:
            case VariableDefinitionNode:
                return base.VisitAssign(node, context);
        }
        
        var exceptionRecord = PlampSemanticsExceptions.InvalidAssignmentTarget();
        SetExceptionToNodeAndChildren(exceptionRecord, node.Left, context);
        return base.VisitAssign(node, context);
    }

    #endregion

    #region Binary

    protected override VisitResult VisitBinaryExpression(BaseBinaryNode binaryNode, ImpossibleTreeValidatorContext context)
    {
        if (binaryNode.Left == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingLeftExpression();
            SetExceptionToNode(exceptionRecord, binaryNode, context);
        }

        if (binaryNode.Right == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingRightExpression();
            SetExceptionToNode(exceptionRecord, binaryNode, context);
        }
        
        _ = ValidateBinaryBranch(binaryNode.Left, context);
        _ = ValidateBinaryBranch(binaryNode.Right, context);
        return VisitResult.Continue;
    }

    #endregion

    #region Body level

    protected override VisitResult VisitBody(BodyNode node, ImpossibleTreeValidatorContext context)
    {
        //TODO: Complete visitor
    }

    protected override VisitResult VisitFor(ForNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingForBody();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        AddForChildException(PlampSemanticsExceptions.InvalidForLoopCounter(), node.Counter);

        if (node.IteratorVar != null && node.IteratorVar is not AssignNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.InvalidForIterator();
            SetExceptionToNode(exceptionRecord, node.IteratorVar, context);
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
                    SetExceptionToNode(record, child, context);
                    break;
            }
        }
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitForeach(ForeachNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingForeachBody();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Iterator == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ForeachIteratorVarMustExist();
            SetExceptionToNode(exceptionRecord, node, context);
        }
        
        switch (node.Iterator)
        {
            case null:
            case VariableDefinitionNode:
                break;
            default:
                var exceptionRecord = PlampSemanticsExceptions.ForeachIteratorMustBeVariableDeclaration();
                SetExceptionToNode(exceptionRecord, node.Iterator, context);
                break;
        }

        if (node.Iterable == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ForeachIterableMustExist();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        switch (node.Iterable)
        {
            case null:
            case MemberNode:
            case MemberAccessNode:
            case CallNode:
            case IndexerNode:
            case ConstructorCallNode:
            case CastNode:
                break;
            default:
                var exceptionRecord = PlampSemanticsExceptions.MismatchForeachIterableNodeType();
                SetExceptionToNode(exceptionRecord, node.Iterable, context);
                break;
        }
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitWhile(WhileNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingWhileBody();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Condition == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MissingWhileCondition();
            SetExceptionToNode(exceptionRecord, node, context);
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
                SetExceptionToNode(exceptionRecord, node.Condition, context);
                return VisitResult.Continue;
        }
    }

    protected override VisitResult VisitCondition(ConditionNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.IfClause != null) return VisitResult.Continue;
        var exceptionRecord = PlampSemanticsExceptions.ConditionMustHaveBaseClause();
        SetExceptionToNode(exceptionRecord, node, context);

        return VisitResult.Continue;
    }

    protected override VisitResult VisitClause(ClauseNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Body == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ConditionClauseMustHaveBody();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        switch (node.Predicate)
        {
            case null:
                var exceptionRecord = PlampSemanticsExceptions.ClausePredicateMustExist();
                SetExceptionToNode(exceptionRecord, node, context);
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
                SetExceptionToNode(exceptionRecord, node.Predicate, context);
                return VisitResult.Continue;
        }
    }

    protected override VisitResult VisitBreak(BreakNode node, ImpossibleTreeValidatorContext context)
    {
        return VisitResult.Continue;
    }

    protected override VisitResult VisitReturn(ReturnNode node, ImpossibleTreeValidatorContext context)
    {
        //Return can be made from a void method
        return VisitResult.Continue;
    }

    protected override VisitResult VisitContinue(ContinueNode node, ImpossibleTreeValidatorContext context)
    {
        return VisitResult.Continue;
    }

    #endregion

    #region Unary

    protected override VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, ImpossibleTreeValidatorContext context)
    {
        if (unaryNode.Inner == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.UnaryNodeRequiresUnderlyingNode();
            SetExceptionToNode(exceptionRecord, unaryNode, context);
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
                SetExceptionToNode(exceptionRecord, unaryNode, context);
                return VisitResult.Continue;
        }
    }

    #endregion

    #region Object members(eg. call, indexer, prop)

    protected override VisitResult VisitCall(CallNode node, ImpossibleTreeValidatorContext context)
    {
        ValidateFromMemberInMemberInteraction(node, node.From, 
            PlampSemanticsExceptions.CallNodeRequireCallerNode(),
            PlampSemanticsExceptions.CallerNodeTypeMismatch(),
            context);

        ValidateArgsInMemberInteraction(node, node.Args,
            PlampSemanticsExceptions.ArgNodeMustNotBeNull(),
            PlampSemanticsExceptions.ArgMustBeValueReturningNode(),
            context);
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitIndexer(IndexerNode node, ImpossibleTreeValidatorContext context)
    {
        ValidateFromMemberInMemberInteraction(node, node.ToIndex, 
            PlampSemanticsExceptions.IndexNodeRequireIndexableNode(),
            PlampSemanticsExceptions.IndexableNodeTypeMismatch(),
            context);

        ValidateArgsInMemberInteraction(node, node.Arguments,
            PlampSemanticsExceptions.ArgNodeMustNotBeNull(),
            PlampSemanticsExceptions.IndexerArgMustBeReturningNode(),
            context);
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitMemberAccess(MemberAccessNode accessNode, ImpossibleTreeValidatorContext context)
    {
        if (accessNode.From == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.CannotAccessMemberFromNothing();
            SetExceptionToNode(exceptionRecord, accessNode, context);
        }

        if (accessNode.Member == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.CannotAccessNothing();
            SetExceptionToNode(exceptionRecord, accessNode, context);
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
                SetExceptionToNode(exceptionRecord, accessNode.From, context);
                break;
        }

        if (accessNode.Member is not MemberNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.MemberAccessTargetMustBeMember();
            SetExceptionToNode(exceptionRecord, accessNode.Member, context);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult VisitConstructor(ConstructorCallNode callNode, ImpossibleTreeValidatorContext context)
    {
        if (callNode.Type == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ConstructorMustHaveCreatingType();
            SetExceptionToNode(exceptionRecord, callNode, context);
        }

        if (callNode.Type is not TypeNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.ConstructorTargetMustBeType();
            SetExceptionToNode(exceptionRecord, callNode.Type, context);
        }
        
        ValidateArgsInMemberInteraction(callNode, callNode.Args,
            PlampSemanticsExceptions.ArgNodeMustNotBeNull(),
            PlampSemanticsExceptions.ConstructorArgTypeMismatch(),
            context);
        return VisitResult.Continue;
    }

    protected override VisitResult VisitCast(CastNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.ToType is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.CastMustHaveType();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.ToType is not TypeNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.CastTargetTypeMustBeTypeNode();
            SetExceptionToNode(exceptionRecord, node.ToType, context);
        }

        switch (node.Inner)
        {
            case null:
                var exceptionRecord = PlampSemanticsExceptions.NullCastTarget();
                SetExceptionToNode(exceptionRecord, node, context);
                return VisitResult.Continue;
            case BaseBinaryNode:
            case BaseUnaryNode:
            case CastNode:
            case LiteralNode:
            case CallNode:
            case MemberNode:
            case IndexerNode:
            case MemberAccessNode:
                if(node.Inner is BaseAssignNode) goto default;
                return VisitResult.Continue;
            default:
                exceptionRecord = PlampSemanticsExceptions.CastTargetMustReturnValue();
                SetExceptionToNode(exceptionRecord, node.Inner, context);
                return VisitResult.Continue;
        }
    }

    #endregion

    #region Global

    protected override VisitResult VisitDef(DefNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.ReturnType is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.DefNodeMustHaveReturnType();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.ReturnType is not TypeNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.DefNodeReturnTypeMustBeTypeNode();
            SetExceptionToNode(exceptionRecord, node.ReturnType, context);
        }

        if (node.Name is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MethodMustHaveName();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Name is not MemberNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.MethodNameMustBeMemberName();
            SetExceptionToNode(exceptionRecord, node.Name, context);
        }

        var hasNull = false;
        foreach (var parameter in node.ParameterList)
        {
            if (parameter is null)
            {
                hasNull = true;
                continue;
            }

            if (parameter is ParameterNode) continue;
            var exceptionRecord = PlampSemanticsExceptions.MethodArgNodeMustBeParameter();
            SetExceptionToNode(exceptionRecord, parameter, context);
        }

        if (hasNull)
        {
            var exceptionRecord = PlampSemanticsExceptions.ArgNodeMustNotBeNull();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Body is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.MethodMustHaveBody();
            SetExceptionToNode(exceptionRecord, node, context);
        }
        
        return VisitResult.Continue;
    }

    protected override VisitResult VisitUse(UseNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Namespace == null)
        {
            var exceptionRecord = PlampSemanticsExceptions.UseMustHasTargetModule();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Namespace is not MemberNode or MemberAccessNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.UseTargetMustBeMember();
            SetExceptionToNode(exceptionRecord, node.Namespace, context);
        }
        
        return VisitResult.Continue;
    }

    #endregion

    #region Misc(arg, type)

    protected override VisitResult VisitType(TypeNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.TypeName is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.TypeNodeMustHaveName();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.TypeName is not MemberNode or MemberAccessNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.TypeNameMustBeMember();
            SetExceptionToNode(exceptionRecord, node.TypeName, context);
        }

        if (node.InnerGenerics is null) return VisitResult.Continue;

        var hasNull = false;
        foreach (var generic in node.InnerGenerics)
        {
            if (generic is null)
            {
                hasNull = true;
                continue;
            }
            if(generic is TypeNode) continue;
            var exceptionRecord = PlampSemanticsExceptions.GenericMustBeType();
            SetExceptionToNode(exceptionRecord, generic, context);
        }

        if (hasNull)
        {
            var exceptionRecord = PlampSemanticsExceptions.GenericsMustNotBeNull();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult VisitParameter(ParameterNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Name is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ParameterMustHaveName();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Name is not MemberNode or MemberAccessNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.ParameterNameMustBeMember();
            SetExceptionToNode(exceptionRecord, node.Name, context);
        }

        if (node.Type is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.ParameterMustHaveType();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Type is not TypeNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.ParameterTypeMustBeTypeNode();
            SetExceptionToNode(exceptionRecord, node.Type, context);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult VisitVariableDefinition(VariableDefinitionNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.Member is null)
        {
            var exceptionRecord = PlampSemanticsExceptions.VariableMustHaveName();
            SetExceptionToNode(exceptionRecord, node, context);
        }

        if (node.Member is not MemberNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.VariableNameMustBeMember();
            SetExceptionToNode(exceptionRecord, node.Member, context);
        }
        
        if(node.Type is null) return VisitResult.Continue;

        if (node.Type is not TypeNode)
        {
            var exceptionRecord = PlampSemanticsExceptions.VariableTypeMustBeTypeNode();
            SetExceptionToNode(exceptionRecord, node.Type, context);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult VisitMember(MemberNode node, ImpossibleTreeValidatorContext context)
    {
        if (node.MemberName is not null) return VisitResult.Continue;
        
        var exceptionRecord = PlampSemanticsExceptions.MemberNameIsNull();
        SetExceptionToNode(exceptionRecord, node, context);

        return VisitResult.Continue;
    }

    protected override VisitResult VisitLiteral(LiteralNode literalNode, ImpossibleTreeValidatorContext context)
    {
        //Value of literal can be anything if type is void(void is nothing)
        if(literalNode.Type == typeof(void) || literalNode.Type is null) return VisitResult.Continue;
        
        if (literalNode.Type.IsValueType && 
            (literalNode.Value == null || !literalNode.Value.GetType().IsValueType))
        {
            var exceptionRecord = PlampSemanticsExceptions.LiteralIsNotValueType();
            SetExceptionToNode(exceptionRecord, literalNode, context);
            return VisitResult.Continue;
        }

        //Only for reference types
        if (!literalNode.Type.IsValueType && literalNode.Type != literalNode.Value?.GetType())
        {
            var exceptionRecord = PlampSemanticsExceptions.LiteralTypeMismatch();
            SetExceptionToNode(exceptionRecord, literalNode, context);
        }
        
        return VisitResult.Continue;
    }

    #endregion

    #region Shared rules

    private VisitResult ValidateBinaryBranch(NodeBase leaf, ImpossibleTreeValidatorContext context)
    {
        switch (leaf)
        {
            case BaseBinaryNode bin:
                if(bin is BaseAssignNode) break;
                return VisitResult.Continue;
            case CallNode:
            case ConstructorCallNode:
            case IndexerNode:
            case MemberAccessNode:
            case CastNode:
            case BaseUnaryNode:
            case LiteralNode:
            case MemberNode:
                return VisitResult.Continue;
        }

        var exceptionRecord = PlampSemanticsExceptions.InvalidChildExpression();
        SetExceptionToNodeAndChildren(exceptionRecord, leaf, context);
        
        return VisitResult.Continue;
    }

    private void ValidateFromMemberInMemberInteraction(NodeBase parent, NodeBase from, 
        PlampExceptionRecord nullException, PlampExceptionRecord typeMismatch, ImpossibleTreeValidatorContext context)
    {
        if (from == null)
        {
            SetExceptionToNode(nullException, parent, context);
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
                SetExceptionToNode(typeMismatch, from, context);
                break;
        }
    }

    private void ValidateArgsInMemberInteraction(NodeBase parent, List<NodeBase> args, 
        PlampExceptionRecord possibleArgNull, PlampExceptionRecord typeMismatch,
        ImpossibleTreeValidatorContext context)
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
                    SetExceptionToNode(typeMismatch, arg, context);
                    continue;
            }
        }

        if (!hasNullArg) return;
        SetExceptionToNode(possibleArgNull, parent, context);
    }
    
    #endregion

    #region Helper logic

    private void SetExceptionToNodeAndChildren(PlampExceptionRecord exceptionRecord, NodeBase node, ImpossibleTreeValidatorContext context)
    {
        var exception = context.Table.SetExceptionToNodeAndChildren(exceptionRecord, node, context.FileName, context.AssemblyName);
        context.Exceptions.Add(exception);
        context.SkippedBranches.Add(node);
    }

    private void SetExceptionToNode(PlampExceptionRecord exceptionRecord, NodeBase node, ImpossibleTreeValidatorContext context)
    {
        var exception = context.Table.SetExceptionToNodeWithoutChildren(exceptionRecord, node, context.FileName, context.AssemblyName);
        context.Exceptions.Add(exception);
        context.SkippedBranches.Add(node);
    }

    #endregion
}