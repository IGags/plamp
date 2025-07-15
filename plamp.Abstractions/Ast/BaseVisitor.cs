using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Unary;

namespace plamp.Abstractions.Ast;

//TODO: Possible do with stack if will StackOverflow occurs
public abstract class BaseVisitor<TContext>
{
    protected enum VisitResult
    {
        Continue,
        Break,
        SkipChildren
    }
    
    protected virtual VisitResult VisitInternal(NodeBase node, TContext context)
    {
        var res = VisitNodeBase(node, context);
        
        switch (res)
        {
            case VisitResult.Break: return VisitResult.Break;
            case VisitResult.SkipChildren: return VisitResult.Continue;
            default:
                res = VisitChildren(node, context);
                return res == VisitResult.Break ? VisitResult.Break : VisitResult.Continue;
        }
    }

    protected virtual VisitResult VisitChildren(NodeBase node, TContext context)
    {
        foreach (var child in node.Visit())
        {
            var res = VisitInternal(child, context);
            if(res == VisitResult.Break) return VisitResult.Break;
        }
        return VisitResult.Continue;
    }
    
    protected virtual VisitResult VisitNodeBase(NodeBase node, TContext context)
    {
        switch (node)
        {
            case RootNode rootNode:
                return VisitRoot(rootNode, context);
            case ModuleDefinitionNode moduleDefinition:
                return VisitModuleDefinition(moduleDefinition, context);
            case ImportNode importNode:
                return VisitImport(importNode, context);
            case ImportItemNode importItem:
                return VisitImportItem(importItem, context);
            case BodyNode bodyNode:
                return VisitBody(bodyNode, context);
            case ConditionNode conditionNode:
                return VisitCondition(conditionNode, context);
            case DefNode defNode:
                return VisitDef(defNode, context);
            case WhileNode whileNode:
                return VisitWhile(whileNode, context);
            case BreakNode breakNode:
                return VisitBreak(breakNode, context);
            case ContinueNode continueNode:
                return VisitContinue(continueNode, context);
            case ReturnNode returnNode:
                return VisitReturn(returnNode, context);
            case CallNode callNode:
                return VisitCall(callNode, context);
            case CastNode castNode:
                return VisitCast(castNode, context);
            case ConstructorCallNode constructorNode:
                return VisitConstructor(constructorNode, context);
            case EmptyNode emptyNode:
                return VisitEmpty(emptyNode, context);
            case MemberNode memberNode:
                return VisitMember(memberNode, context);
            case ParameterNode parameterNode:
                return VisitParameter(parameterNode, context);
            case TypeNode typeNode:
                return VisitType(typeNode, context);
            case VariableDefinitionNode variableDefinitionNode:
                return VisitVariableDefinition(variableDefinitionNode, context);
            case MemberAccessNode memberAccessNode:
                return VisitMemberAccess(memberAccessNode, context);
            case ThisNode thisNode:
                return VisitThis(thisNode, context);
            case LiteralNode constNode:
                return VisitLiteral(constNode, context);
            case BaseBinaryNode binaryNode:
                return VisitBinaryExpression(binaryNode, context);
        }
        
        return VisitDefault(node, context);
    }

    protected virtual VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, TContext context)
    {
        switch (unaryNode)
        {
            case NotNode notNode:
                return VisitNot(notNode, context);
            case PostfixDecrementNode postfixDecrement:
                return VisitPostfixDecrement(postfixDecrement, context);
            case PostfixIncrementNode postfixIncrement:
                return VisitPostfixIncrement(postfixIncrement, context);
            case PrefixDecrementNode prefixDecrementNode:
                return VisitPrefixDecrement(prefixDecrementNode, context);
            case PrefixIncrementNode prefixIncrementNode:
                return VisitPrefixIncrement(prefixIncrementNode, context);
            case UnaryMinusNode unaryMinusNode:
                return VisitUnaryMinus(unaryMinusNode, context);
        }
        
        return VisitDefault(unaryNode, context);
    }
    
    protected virtual VisitResult VisitBinaryExpression(BaseBinaryNode node, TContext context)
    {
        switch (node)
        {
            case BaseAssignNode baseAssignNode:
                return VisitBaseAssign(baseAssignNode, context);
            case BitwiseAndNode bitwiseAndNode:
                return VisitBitwiseAnd(bitwiseAndNode, context);
            case BitwiseOrNode bitwiseOrNode:
                return VisitBitwiseOr(bitwiseOrNode, context);
            case XorNode xorNode:
                return VisitXor(xorNode, context);
            case AndNode andNode:
                return VisitAnd(andNode, context);
            case DivideNode divideNode:
                return VisitDivide(divideNode, context);
            case EqualNode equalNode:
                return VisitEqual(equalNode, context);
            case GreaterNode greaterNode:
                return VisitGreater(greaterNode, context);
            case GreaterOrEqualNode greaterOrEqualsNode:
                return VisitGreaterOrEquals(greaterOrEqualsNode, context);
            case LessNode lessNode:
                return VisitLess(lessNode, context);
            case LessOrEqualNode lessOrEqualNode:
                return VisitLessOrEqual(lessOrEqualNode, context);
            case MinusNode minusNode:
                return VisitMinus(minusNode, context);
            case ModuloNode moduloNode:
                return VisitModulo(moduloNode, context);
            case MultiplyNode multiplyNode:
                return VisitMultiply(multiplyNode, context);
            case NotEqualNode notEqualNode:
                return VisitNotEqual(notEqualNode, context);
            case OrNode orNode:
                return VisitOr(orNode, context);
            case PlusNode plusNode:
                return VisitPlus(plusNode, context);
        }
        
        return VisitDefault(node, context);
    }

    protected virtual VisitResult VisitBaseAssign(BaseAssignNode node, TContext context)
    {
        switch (node)
        {
            case AssignNode assignNode:
                return VisitAssign(assignNode, context);
        }

        return VisitDefault(node, context);
    }

    protected virtual VisitResult VisitThis(ThisNode thisNode, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitAssign(AssignNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitAnd(AndNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitDivide(DivideNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitEqual(EqualNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitGreater(GreaterNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitGreaterOrEquals(GreaterOrEqualNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitLess(LessNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitLessOrEqual(LessOrEqualNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMinus(MinusNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitModulo(ModuloNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMultiply(MultiplyNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitNotEqual(NotEqualNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitOr(OrNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPlus(PlusNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBody(BodyNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitCondition(ConditionNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitDef(DefNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitWhile(WhileNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBreak(BreakNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitContinue(ContinueNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitReturn(ReturnNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitNot(NotNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPostfixDecrement(PostfixDecrementNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPostfixIncrement(PostfixIncrementNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPrefixDecrement(PrefixDecrementNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitPrefixIncrement(PrefixIncrementNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitUnaryMinus(UnaryMinusNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitCall(CallNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitCast(CastNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitConstructor(ConstructorCallNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitEmpty(EmptyNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMember(MemberNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitParameter(ParameterNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitType(TypeNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitVariableDefinition(VariableDefinitionNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitMemberAccess(MemberAccessNode accessNode, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitLiteral(LiteralNode literalNode, TContext context) => VisitResult.Continue;

    protected virtual VisitResult VisitBitwiseAnd(BitwiseAndNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBitwiseOr(BitwiseOrNode node, TContext context) => VisitResult.Continue;

    protected virtual VisitResult VisitXor(XorNode node, TContext context) => VisitResult.Continue;
    
    protected virtual VisitResult VisitRoot(RootNode node, TContext context) => VisitResult.Continue;

    protected virtual VisitResult VisitImport(ImportNode node, TContext context) => VisitResult.Continue;

    protected virtual VisitResult VisitImportItem(ImportItemNode node, TContext context) => VisitResult.Continue;

    protected virtual VisitResult VisitModuleDefinition(ModuleDefinitionNode definition, TContext context) => VisitResult.Continue;

    //Continue because possible custom ast node types that compiles in default nodes in future
    protected virtual VisitResult VisitDefault(NodeBase node, TContext context) => VisitResult.Continue;
}