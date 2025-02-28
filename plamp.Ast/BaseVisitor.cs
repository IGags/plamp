using System;
using System.Collections.Generic;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;

namespace plamp.Ast;

//TODO: Possible do with stack if will StackOverflow occurs
public abstract class BaseVisitor
{
    protected enum VisitResult
    {
        Continue,
        Break,
        SkipChildren
    }

    public virtual void Visit(NodeBase node)
    {
        _ = VisitInternal(node);
    }
    
    protected virtual VisitResult VisitInternal(NodeBase node)
    {
        var res = VisitNodeBase(node);
        
        if (res == VisitResult.Break) return VisitResult.Break;
        if (res == VisitResult.SkipChildren) return VisitResult.Continue;
        
        var children = node.Visit();
        res = VisitChildren(children);
        
        return res == VisitResult.Break ? VisitResult.Break : VisitResult.Continue;
    }

    protected virtual VisitResult VisitChildren(IEnumerable<NodeBase> children)
    {
        foreach (var child in children)
        {
            var res = VisitInternal(child);
            if(res == VisitResult.Break) return VisitResult.Break;
        }
        return VisitResult.Continue;
    }
    
    protected virtual VisitResult VisitNodeBase(NodeBase node)
    {
        if (node == null) return VisitNull();
        
        switch (node)
        {
            case BodyNode bodyNode:
                return VisitBody(bodyNode);
            case ClauseNode clauseNode:
                return VisitClause(clauseNode);
            case ConditionNode conditionNode:
                return VisitCondition(conditionNode);
            case DefNode defNode:
                return VisitDef(defNode);
            case ForeachNode forNode:
                return VisitFor(forNode);
            case WhileNode whileNode:
                return VisitWhile(whileNode);
            case BreakNode breakNode:
                return VisitBreak(breakNode);
            case ContinueNode continueNode:
                return VisitContinue(continueNode);
            case ReturnNode returnNode:
                return VisitReturn(returnNode);
            case CallNode callNode:
                return VisitCall(callNode);
            case CastNode castNode:
                return VisitCast(castNode);
            case ConstructorNode constructorNode:
                return VisitConstructor(constructorNode);
            case EmptyNode emptyNode:
                return VisitEmpty(emptyNode);
            case IndexerNode indexerNode:
                return VisitIndexer(indexerNode);
            case MemberNode memberNode:
                return VisitMember(memberNode);
            case ParameterNode parameterNode:
                return VisitParameter(parameterNode);
            case TypeNode typeNode:
                return VisitType(typeNode);
            case VariableDefinitionNode variableDefinitionNode:
                return VisitVariableDefinition(variableDefinitionNode);
            case UseNode useNode:
                return VisitUse(useNode);
            case MemberAccessNode memberAccessNode:
                return VisitMemberAccess(memberAccessNode);
            case LiteralNode constNode:
                return VisitLiteral(constNode);
            case BaseBinaryNode binaryNode:
                return VisitBinaryExpression(binaryNode);
        }
        
        return VisitDefault(node);
    }

    protected virtual VisitResult VisitUnaryNode(BaseUnaryNode unaryNode)
    {
        switch (unaryNode)
        {
            case NotNode notNode:
                return VisitNot(notNode);
            case PostfixDecrementNode postfixDecrement:
                return VisitPostfixDecrement(postfixDecrement);
            case PostfixIncrementNode postfixIncrement:
                return VisitPostfixIncrement(postfixIncrement);
            case PrefixDecrementNode prefixDecrementNode:
                return VisitPrefixDecrement(prefixDecrementNode);
            case PrefixIncrementNode prefixIncrementNode:
                return VisitPrefixIncrement(prefixIncrementNode);
            case UnaryMinusNode unaryMinusNode:
                return VisitUnaryMinus(unaryMinusNode);
        }
        
        return VisitDefault(unaryNode);
    }
    
    protected virtual VisitResult VisitBinaryExpression(BaseBinaryNode binaryNode)
    {
        switch (binaryNode)
        {
            case BaseAssignNode baseAssignNode:
                return VisitBaseAssign(baseAssignNode);
            case BitwiseAndNode bitwiseAndNode:
                return VisitBitwiseAnd(bitwiseAndNode);
            case BitwiseOrNode bitwiseOrNode:
                return VisitBitwiseOr(bitwiseOrNode);
            case XorNode xorNode:
                return VisitXor(xorNode);
            case AndNode andNode:
                return VisitAnd(andNode);
            case DivideNode divideNode:
                return VisitDivide(divideNode);
            case EqualNode equalNode:
                return VisitEqual(equalNode);
            case GreaterNode greaterNode:
                return VisitGreater(greaterNode);
            case GreaterOrEqualsNode greaterOrEqualsNode:
                return VisitGreaterOrEquals(greaterOrEqualsNode);
            case LessNode lessNode:
                return VisitLess(lessNode);
            case LessOrEqualNode lessOrEqualNode:
                return VisitLessOrEqual(lessOrEqualNode);
            case MinusNode minusNode:
                return VisitMinus(minusNode);
            case ModuloNode moduloNode:
                return VisitModulo(moduloNode);
            case MultiplyNode multiplyNode:
                return VisitMultiply(multiplyNode);
            case NotEqualNode notEqualNode:
                return VisitNotEqual(notEqualNode);
            case OrNode orNode:
                return VisitOr(orNode);
            case PlusNode plusNode:
                return VisitPlus(plusNode);
        }
        
        return VisitDefault(binaryNode);
    }

    protected virtual VisitResult VisitBaseAssign(BaseAssignNode node)
    {
        switch (node)
        {
            case AndAndAssignNode andAndAssignNode:
                return VisitAndAndAssign(andAndAssignNode);
            case OrAndAssignNode orAndAssignNode:
                return VisitOrAndAssign(orAndAssignNode);
            case XorAndAssignNode xorAndAssignNode:
                return VisitXorAndAssign(xorAndAssignNode);
            case AddAndAssignNode addAndAssignNode:
                return VisitAddAndAssign(addAndAssignNode);
            case AssignNode assignNode:
                return VisitAssign(assignNode);
            case DivAndAssignNode divAndAssignNode:
                return VisitDivAndAssign(divAndAssignNode);
            case ModuloAndAssignNode moduloAndAssignNode:
                return VisitModuloAndAssign(moduloAndAssignNode);
            case MulAndAssignNode mulAndAssignNode:
                return VisitMulAndAssign(mulAndAssignNode);
            case SubAndAssignNode subAndAssignNode:
                return VisitSubAndAssign(subAndAssignNode);
        }

        return VisitDefault(node);
    }

    protected virtual VisitResult VisitNull() => VisitResult.Continue;

    protected virtual VisitResult VisitAddAndAssign(AddAndAssignNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitAssign(AssignNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitDivAndAssign(DivAndAssignNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitModuloAndAssign(ModuloAndAssignNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMulAndAssign(MulAndAssignNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitSubAndAssign(SubAndAssignNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitAnd(AndNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitDivide(DivideNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitEqual(EqualNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitGreater(GreaterNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitGreaterOrEquals(GreaterOrEqualsNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitLess(LessNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitLessOrEqual(LessOrEqualNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMinus(MinusNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitModulo(ModuloNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMultiply(MultiplyNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitNotEqual(NotEqualNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitOr(OrNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPlus(PlusNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBody(BodyNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitClause(ClauseNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitCondition(ConditionNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitDef(DefNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitFor(ForeachNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitWhile(WhileNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBreak(BreakNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitContinue(ContinueNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitReturn(ReturnNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitNot(NotNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPostfixDecrement(PostfixDecrementNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPostfixIncrement(PostfixIncrementNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPrefixDecrement(PrefixDecrementNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPrefixIncrement(PrefixIncrementNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitUnaryMinus(UnaryMinusNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitCall(CallNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitCast(CastNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitConstructor(ConstructorNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitEmpty(EmptyNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitIndexer(IndexerNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMember(MemberNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitParameter(ParameterNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitType(TypeNode node) => VisitResult.Continue;
    
    protected virtual VisitResult VisitVariableDefinition(VariableDefinitionNode node) => VisitResult.Continue;

    protected virtual VisitResult VisitUse(UseNode node)  => VisitResult.Continue;
    
    protected virtual VisitResult VisitMemberAccess(MemberAccessNode accessNode) => VisitResult.Continue;
    
    protected virtual VisitResult VisitLiteral(LiteralNode literalNode) => VisitResult.Continue;

    protected virtual VisitResult VisitAndAndAssign(AndAndAssignNode andAndAssign) => VisitResult.Continue;

    protected virtual VisitResult VisitOrAndAssign(OrAndAssignNode orAndAssign) => VisitResult.Continue;

    protected virtual VisitResult VisitXorAndAssign(XorAndAssignNode xorAndAssign) => VisitResult.Continue;

    protected virtual VisitResult VisitBitwiseAnd(BitwiseAndNode bitwiseAnd) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBitwiseOr(BitwiseOrNode bitwiseOr) => VisitResult.Continue;

    protected virtual VisitResult VisitXor(XorNode xor) => VisitResult.Continue;
    
    //Continue because possible custom ast node types that compiles in default nodes in future
    protected virtual VisitResult VisitDefault(NodeBase node) => VisitResult.Continue;
}