using System.Linq;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;

namespace plamp.Ast;

public abstract class BaseVisitor
{
    public void Visit(NodeBase node)
    {
        VisitNode(node);
        VisitNodeBase(node);
        var children = node.Visit();
        foreach (var child in children.Where(x => x != null))
        {
            Visit(child);
        }
    }

    private void VisitNode(NodeBase node)
    {
        switch (node)
        {
            case AddAndAssignNode addAndAssignNode:
                VisitAddAndAssign(addAndAssignNode);
                return;
            case AssignNode assignNode:
                VisitAssign(assignNode);
                return;
            case CreateAndAssignNode createAndAssignNode:
                VisitCreateAndAssign(createAndAssignNode);
                return;
            case DivAndAssignNode divAndAssignNode:
                VisitDivAndAssign(divAndAssignNode);
                return;
            case ModuloAndAssignNode moduloAndAssignNode:
                VisitModuloAndAssign(moduloAndAssignNode);
                return;
            case MulAndAssignNode mulAndAssignNode:
                VisitMulAndAssign(mulAndAssignNode);
                return;
            case SubAndAssignNode subAndAssignNode:
                VisitSubAndAssign(subAndAssignNode);
                return;
            case AndNode andNode:
                VisitAnd(andNode);
                return;
            case DivideNode divideNode:
                VisitDivide(divideNode);
                return;
            case EqualNode equalNode:
                VisitEqual(equalNode);
                return;
            case GreaterNode greaterNode:
                VisitGreater(greaterNode);
                return;
            case GreaterOrEqualsNode greaterOrEqualsNode:
                VisitGreaterOrEquals(greaterOrEqualsNode);
                return;
            case LessNode lessNode:
                VisitLess(lessNode);
                return;
            case LessOrEqualNode lessOrEqualNode:
                VisitLessOrEqual(lessOrEqualNode);
                return;
            case MinusNode minusNode:
                VisitMinus(minusNode);
                return;
            case ModuloNode moduloNode:
                VisitModulo(moduloNode);
                return;
            case MultiplyNode multiplyNode:
                VisitMultiply(multiplyNode);
                return;
            case NotEqualNode notEqualNode:
                VisitNotEqual(notEqualNode);
                return;
            case OrNode orNode:
                VisitOr(orNode);
                return;
            case PlusNode plusNode:
                VisitPlus(plusNode);
                return;
            case BodyNode bodyNode:
                VisitBody(bodyNode);
                return;
            case ClauseNode clauseNode:
                VisitClause(clauseNode);
                return;
            case ConditionNode conditionNode:
                VisitCondition(conditionNode);
                return;
            case DefNode defNode:
                VisitDef(defNode);
                return;
            case ForNode forNode:
                VisitFor(forNode);
                return;
            case WhileNode whileNode:
                VisitWhile(whileNode);
                return;
            case BreakNode breakNode:
                VisitBreak(breakNode);
                return;
            case ContinueNode continueNode:
                VisitContinue(continueNode);
                return;
            case ReturnNode returnNode:
                VisitReturn(returnNode);
                return;
            case NotNode notNode:
                VisitNot(notNode);
                return;
            case PostfixDecrementNode postfixDecrement:
                VisitPostfixDecrement(postfixDecrement);
                return;
            case PostfixIncrementNode postfixIncrement:
                VisitPostfixIncrement(postfixIncrement);
                return;
            case PrefixDecrementNode prefixDecrementNode:
                VisitPrefixDecrement(prefixDecrementNode);
                return;
            case PrefixIncrementNode prefixIncrementNode:
                VisitPrefixIncrement(prefixIncrementNode);
                return;
            case UnaryMinusNode unaryMinusNode:
                VisitUnaryMinus(unaryMinusNode);
                return;
            case CallNode callNode:
                VisitCall(callNode);
                return;
            case CastNode castNode:
                VisitCast(castNode);
                return;
            case ConstructorNode constructorNode:
                VisitConstructor(constructorNode);
                return;
            case EmptyNode emptyNode:
                VisitEmpty(emptyNode);
                return;
            case IndexerNode indexerNode:
                VisitIndexer(indexerNode);
                return;
            case MemberNode memberNode:
                VisitMember(memberNode);
                return;
            case ParameterNode parameterNode:
                VisitParameter(parameterNode);
                return;
            case TypeNode typeNode:
                VisitType(typeNode);
                return;
            case VariableDefinitionNode variableDefinitionNode:
                VisitVariableDefinition(variableDefinitionNode);
                return;
            case UseNode useNode:
                VisitUse(useNode);
                return;
            case MemberAccessNode memberAccessNode:
                VisitMemberAccess(memberAccessNode);
                return;
            case ConstNode constNode:
                VisitConst(constNode);
                return;
            case AndAndAssignNode andAndAssignNode:
                VisitAndAndAssign(andAndAssignNode);
                return;
            case OrAndAssignNode orAndAssignNode:
                VisitOrAndAssign(orAndAssignNode);
                return;
            case XorAndAssignNode xorAndAssignNode:
                VisitXorAndAssign(xorAndAssignNode);
                return;
            case BitwiseAndNode bitwiseAndNode:
                VisitBitwiseAnd(bitwiseAndNode);
                return;
            case BitwiseOrNode bitwiseOrNode:
                VisitBitwiseOr(bitwiseOrNode);
                return;
            case XorNode xorNode:
                VisitXor(xorNode);
                return;
        }
    }
    
    protected virtual void VisitAddAndAssign(AddAndAssignNode node){}
    
    protected virtual void VisitAssign(AssignNode node){}
    
    protected virtual void VisitCreateAndAssign(CreateAndAssignNode node){}
    
    protected virtual void VisitDivAndAssign(DivAndAssignNode node){}
    
    protected virtual void VisitModuloAndAssign(ModuloAndAssignNode node){}
    
    protected virtual void VisitMulAndAssign(MulAndAssignNode node){}
    
    protected virtual void VisitSubAndAssign(SubAndAssignNode node){}
    
    protected virtual void VisitAnd(AndNode node){}
    
    protected virtual void VisitDivide(DivideNode node){}
    
    protected virtual void VisitEqual(EqualNode node){}
    
    protected virtual void VisitGreater(GreaterNode node){}
    
    protected virtual void VisitGreaterOrEquals(GreaterOrEqualsNode node){}
    
    protected virtual void VisitLess(LessNode node){}
    
    protected virtual void VisitLessOrEqual(LessOrEqualNode node){}
    
    protected virtual void VisitMinus(MinusNode node){}
    
    protected virtual void VisitModulo(ModuloNode node){}
    
    protected virtual void VisitMultiply(MultiplyNode node){}
    
    protected virtual void VisitNotEqual(NotEqualNode node){}
    
    protected virtual void VisitOr(OrNode node){}
    
    protected virtual void VisitPlus(PlusNode node){}
    
    protected virtual void VisitBody(BodyNode node){}
    
    protected virtual void VisitClause(ClauseNode node){}
    
    protected virtual void VisitCondition(ConditionNode node){}
    
    protected virtual void VisitDef(DefNode node){}
    
    protected virtual void VisitFor(ForNode node){}
    
    protected virtual void VisitWhile(WhileNode node){}
    
    protected virtual void VisitBreak(BreakNode node){}
    
    protected virtual void VisitContinue(ContinueNode node){}
    
    protected virtual void VisitReturn(ReturnNode node){}
    
    protected virtual void VisitNot(NotNode node){}
    
    protected virtual void VisitPostfixDecrement(PostfixDecrementNode node){}
    
    protected virtual void VisitPostfixIncrement(PostfixIncrementNode node){}
    
    protected virtual void VisitPrefixDecrement(PrefixDecrementNode node){}
    
    protected virtual void VisitPrefixIncrement(PrefixIncrementNode node){}
    
    protected virtual void VisitUnaryMinus(UnaryMinusNode node){}
    
    protected virtual void VisitCall(CallNode node){}
    
    protected virtual void VisitCast(CastNode node){}
    
    protected virtual void VisitConstructor(ConstructorNode node){}
    
    protected virtual void VisitEmpty(EmptyNode node){}
    
    protected virtual void VisitIndexer(IndexerNode node){}
    
    protected virtual void VisitMember(MemberNode node){}
    
    protected virtual void VisitParameter(ParameterNode node){}
    
    protected virtual void VisitType(TypeNode node){}
    
    protected virtual void VisitVariableDefinition(VariableDefinitionNode node){}

    protected virtual void VisitUse(UseNode node) {}
    
    protected virtual void VisitMemberAccess(MemberAccessNode accessNode){}
    
    protected virtual void VisitConst(ConstNode constNode){}

    protected virtual void VisitAndAndAssign(AndAndAssignNode andAndAssign){}

    protected virtual void VisitOrAndAssign(OrAndAssignNode orAndAssign){}

    protected virtual void VisitXorAndAssign(XorAndAssignNode xorAndAssign){}

    protected virtual void VisitBitwiseAnd(BitwiseAndNode bitwiseAnd){}
    
    protected virtual void VisitBitwiseOr(BitwiseOrNode bitwiseOr){}

    protected virtual void VisitXor(XorNode xor){}
    
    protected virtual void VisitNodeBase(NodeBase node){}
}