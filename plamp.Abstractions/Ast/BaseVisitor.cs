using System;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
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

    protected virtual VisitResult VisitChildren(NodeBase node, TContext context)
    {
        foreach (var child in node.Visit())
        {
            var preVisitRes = VisitResult.Continue;
            if (node is BodyNode)
            {
                preVisitRes = PreVisitInstruction(child, context, node);
                if (preVisitRes == VisitResult.Break) return VisitResult.Break;
            }

            var visitRes = VisitResult.Continue;
            if(preVisitRes is not VisitResult.SkipChildren) visitRes = VisitNodeBase(child, context, node);
            if (visitRes is VisitResult.Break) return VisitResult.Break;
            
            
            if (node is BodyNode) visitRes = PostVisitInstruction(child, context, node);
            if (visitRes == VisitResult.Break) return VisitResult.Break;
        }
        return VisitResult.Continue;
    }
    
    protected virtual VisitResult VisitNodeBase(NodeBase node, TContext context, NodeBase? parent)
    {
        switch (node)
        {
            case RootNode rootNode:
                return VisitCore(rootNode, context, parent, PreVisitRoot, PostVisitRoot);
            case ModuleDefinitionNode moduleDefinition:
                return VisitCore(moduleDefinition, context, parent, PreVisitModuleDefinition, PostVisitModuleDefinition);
            case ImportNode importNode:
                return VisitCore(importNode, context, parent, PreVisitImport, PostVisitImport);
            case ImportItemNode importItem:
                return VisitCore(importItem, context, parent, PreVisitImportItem, PostVisitImportItem);
            case BodyNode bodyNode:
                return VisitCore(bodyNode, context, parent, PreVisitBody, PostVisitBody);
            case ConditionNode conditionNode:
                return VisitCore(conditionNode, context, parent, PreVisitCondition, PostVisitCondition);
            case FuncNode defNode:
                return VisitCore(defNode, context, parent, PreVisitFunction, PostVisitFunction);
            case WhileNode whileNode:
                return VisitCore(whileNode, context, parent, PreVisitWhile, PostVisitWhile);
            case BreakNode breakNode:
                return VisitCore(breakNode, context, parent, PreVisitBreak, PostVisitBreak);
            case ContinueNode continueNode:
                return VisitCore(continueNode, context, parent, PreVisitContinue, PostVisitContinue);
            case ReturnNode returnNode:
                return VisitCore(returnNode, context, parent, PreVisitReturn, PostVisitReturn);
            case CallNode callNode:
                return VisitCore(callNode, context, parent, PreVisitCall, PostVisitCall);
            case CastNode castNode:
                return VisitCore(castNode, context, parent, PreVisitCast, PostVisitCast);
            case ConstructorCallNode constructorNode:
                return VisitCore(constructorNode, context, parent, PreVisitConstructor, PostVisitConstructor);
            case EmptyNode emptyNode:
                return VisitCore(emptyNode, context, parent, PreVisitEmpty, PostVisitEmpty);
            case MemberNode memberNode:
                return VisitCore(memberNode, context, parent, PreVisitMember, PostVisitMember);
            case ParameterNode parameterNode:
                return VisitCore(parameterNode, context, parent, PreVisitParameter, PostVisitParameter);
            case TypeNode typeNode:
                return VisitCore(typeNode, context, parent, PreVisitType, PostVisitType);
            case VariableDefinitionNode variableDefinitionNode:
                return VisitCore(variableDefinitionNode, context, parent, PreVisitVariableDefinition, PostVisitVariableDefinition);
            case MemberAccessNode memberAccessNode:
                return VisitCore(memberAccessNode, context, parent, PreVisitMemberAccess, PostVisitMemberAccess);
            case ThisNode thisNode:
                return VisitCore(thisNode, context, parent, PreVisitThis, PostVisitThis);
            case LiteralNode constNode:
                return VisitCore(constNode, context, parent, PreVisitLiteral, PostVisitLiteral);
            case TypeNameNode typeName:
                return VisitCore(typeName, context, parent, PreVisitTypeName, PostVisitTypeName);
            case FuncNameNode funcName:
                return VisitCore(funcName, context, parent, PreVisitFuncName, PostVisitFuncName);
            case ParameterNameNode parameterName:
                return VisitCore(parameterName, context, parent, PreVisitParameterName, PostVisitParameterName);
            case VariableNameNode variableName:
                return VisitCore(variableName, context, parent, PreVisitVariableName, PostVisitVariableName);
            case BaseBinaryNode binaryNode:
                return VisitBinaryExpression(binaryNode, context, parent);
            case BaseUnaryNode unaryNode:
                return VisitUnaryNode(unaryNode, context, parent);
        }
        
        return VisitCore(node, context, parent, PreVisitDefault, PostVisitDefault);
    }

    protected virtual VisitResult PreVisitUnary(BaseUnaryNode unaryNode, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PostVisitUnary(BaseUnaryNode unaryNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, TContext context, NodeBase? parent)
    {
        var preVisitRes = PreVisitUnary(unaryNode, context, parent);
        if (preVisitRes is VisitResult.Break) return VisitResult.Break;
        var visitRes = VisitResult.Continue;
        if (preVisitRes is not VisitResult.SkipChildren)
        {
            visitRes = unaryNode switch
            {
                NotNode notNode => VisitCore(notNode, context, parent, PreVisitNot, PostVisitNot),
                PostfixDecrementNode postfixDecrement => VisitCore(postfixDecrement, context, parent,
                    PreVisitPostfixDecrement, PostVisitPostfixDecrement),
                PostfixIncrementNode postfixIncrement => VisitCore(postfixIncrement, context, parent,
                    PreVisitPostfixIncrement, PostVisitPostfixIncrement),
                PrefixDecrementNode prefixDecrementNode => VisitCore(prefixDecrementNode, context, parent,
                    PreVisitPrefixDecrement, PostVisitPrefixDecrement),
                PrefixIncrementNode prefixIncrementNode => VisitCore(prefixIncrementNode, context, parent,
                    PreVisitPrefixIncrement, PostVisitPrefixIncrement),
                UnaryMinusNode unaryMinusNode => VisitCore(unaryMinusNode, context, parent, PreVisitUnaryMinus,
                    PostVisitUnaryMinus),
                _ => VisitCore(unaryNode, context, parent, PreVisitDefault, PostVisitDefault)
            };
        }

        return visitRes is VisitResult.Break ? VisitResult.Break : PostVisitUnary(unaryNode, context, parent);
    }
    
    protected virtual VisitResult PreVisitBinary(BaseBinaryNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PostVisitBinary(BaseBinaryNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult VisitBinaryExpression(BaseBinaryNode node, TContext context, NodeBase? parent)
    {
        var preVisitRes = PreVisitBinary(node, context, parent);
        if (preVisitRes is VisitResult.Break) return VisitResult.Break;
        var visitRes = VisitResult.Continue;
        if (preVisitRes is not VisitResult.SkipChildren)
        {
            visitRes = node switch
            {
                BaseAssignNode baseAssignNode => VisitBaseAssign(baseAssignNode, context, parent),
                BitwiseAndNode bitwiseAndNode => VisitCore(bitwiseAndNode, context, parent, PreVisitBitwiseAnd,
                    PostVisitBitwiseAnd),
                BitwiseOrNode bitwiseOrNode => VisitCore(bitwiseOrNode, context, parent, PreVisitBitwiseOr,
                    PostVisitBitwiseOr),
                XorNode xorNode => VisitCore(xorNode, context, parent, PreVisitXor, PostVisitXor),
                AndNode andNode => VisitCore(andNode, context, parent, PreVisitAnd, PostVisitAnd),
                DivNode divideNode => VisitCore(divideNode, context, parent, PreVisitDiv, PostVisitDiv),
                EqualNode equalNode => VisitCore(equalNode, context, parent, PreVisitEqual, PostVisitEqual),
                GreaterNode greaterNode => VisitCore(greaterNode, context, parent, PreVisitGreater, PostVisitGreater),
                GreaterOrEqualNode greaterOrEqualsNode => VisitCore(greaterOrEqualsNode, context, parent,
                    PreVisitGreaterOrEquals, PostVisitGreaterOrEquals),
                LessNode lessNode => VisitCore(lessNode, context, parent, PreVisitLess, PostVisitLess),
                LessOrEqualNode lessOrEqualNode => VisitCore(lessOrEqualNode, context, parent, PreVisitLessOrEqual,
                    PostVisitLessOrEqual),
                SubNode minusNode => VisitCore(minusNode, context, parent, PreVisitSub, PostVisitSub),
                ModuloNode moduloNode => VisitCore(moduloNode, context, parent, PreVisitModulo, PostVisitModulo),
                MulNode multiplyNode => VisitCore(multiplyNode, context, parent, PreVisitMul, PostVisitMul),
                NotEqualNode notEqualNode => VisitCore(notEqualNode, context, parent, PreVisitNotEqual,
                    PostVisitNotEqual),
                OrNode orNode => VisitCore(orNode, context, parent, PreVisitOr, PostVisitOr),
                AddNode plusNode => VisitCore(plusNode, context, parent, PreVisitAdd, PostVisitAdd),
                _ => VisitCore(node, context, parent, PreVisitDefault, PostVisitDefault)
            };
        }

        return visitRes is VisitResult.Break ? VisitResult.Break : PostVisitBinary(node, context, parent);
    }

    protected virtual VisitResult VisitBaseAssign(BaseAssignNode node, TContext context, NodeBase? parent)
    {
        return node switch
        {
            AssignNode assignNode => VisitCore(assignNode, context, parent, PreVisitAssign, PostVisitAssign),
            _ => VisitCore(node, context, parent, PreVisitDefault, PostVisitDefault)
        };
    }

    protected virtual VisitResult PreVisitThis(ThisNode thisNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitThis(ThisNode thisNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitAssign(AssignNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitAssign(AssignNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitAnd(AndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitAnd(AndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitDiv(DivNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitDiv(DivNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitEqual(EqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitEqual(EqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitGreater(GreaterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitGreater(GreaterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitGreaterOrEquals(GreaterOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitGreaterOrEquals(GreaterOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitLess(LessNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitLess(LessNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitLessOrEqual(LessOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitLessOrEqual(LessOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitSub(SubNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitSub(SubNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitModulo(ModuloNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitModulo(ModuloNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitMul(MulNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitMul(MulNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitNotEqual(NotEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitNotEqual(NotEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitOr(OrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitOr(OrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitAdd(AddNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitAdd(AddNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitBody(BodyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitBody(BodyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitCondition(ConditionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitCondition(ConditionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitFunction(FuncNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitFunction(FuncNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitWhile(WhileNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitWhile(WhileNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitBreak(BreakNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitBreak(BreakNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitContinue(ContinueNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitContinue(ContinueNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitReturn(ReturnNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitReturn(ReturnNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitNot(NotNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitNot(NotNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitPostfixDecrement(PostfixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitPostfixDecrement(PostfixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitPostfixIncrement(PostfixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitPostfixIncrement(PostfixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitPrefixDecrement(PrefixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitPrefixDecrement(PrefixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitPrefixIncrement(PrefixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitPrefixIncrement(PrefixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitUnaryMinus(UnaryMinusNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitUnaryMinus(UnaryMinusNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitCall(CallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitCall(CallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitCast(CastNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitCast(CastNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitConstructor(ConstructorCallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitConstructor(ConstructorCallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitEmpty(EmptyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitEmpty(EmptyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitMember(MemberNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitMember(MemberNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitParameter(ParameterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitParameter(ParameterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitType(TypeNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitType(TypeNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitVariableDefinition(VariableDefinitionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitVariableDefinition(VariableDefinitionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitMemberAccess(MemberAccessNode accessNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitMemberAccess(MemberAccessNode accessNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitLiteral(LiteralNode literalNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitLiteral(LiteralNode literalNode, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PreVisitBitwiseAnd(BitwiseAndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitBitwiseAnd(BitwiseAndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitBitwiseOr(BitwiseOrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitBitwiseOr(BitwiseOrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PreVisitXor(XorNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitXor(XorNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitRoot(RootNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitRoot(RootNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PreVisitImport(ImportNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitImport(ImportNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PreVisitImportItem(ImportItemNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitImportItem(ImportItemNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitModuleDefinition(ModuleDefinitionNode definition, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitModuleDefinition(ModuleDefinitionNode definition, TContext context, NodeBase? parent) => VisitResult.Continue;

    //Continue because possible custom ast node types that compiles in default nodes in future
    protected virtual VisitResult PreVisitDefault(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitDefault(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PreVisitInstruction(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitInstruction(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitFuncName(FuncNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitFuncName(FuncNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult PreVisitTypeName(TypeNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitTypeName(TypeNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitParameterName(ParameterNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitParameterName(ParameterNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PreVisitVariableName(VariableNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    protected virtual VisitResult PostVisitVariableName(VariableNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    protected virtual VisitResult VisitCore<T>(
        T node, 
        TContext context, 
        NodeBase? parent,
        Func<T, TContext, NodeBase?, VisitResult> preVisit,
        Func<T, TContext, NodeBase?, VisitResult> postVisit) where T : NodeBase
    {
        var preResult = preVisit(node, context, parent);
        if (preResult is VisitResult.Break) return preResult;
        
        var visitResult = VisitResult.Continue;
        if(preResult is not VisitResult.SkipChildren) visitResult = VisitChildren(node, context);
        if (visitResult == VisitResult.Break) return visitResult;
        
        var postResult = postVisit(node, context, parent);
        return postResult;
    }
}