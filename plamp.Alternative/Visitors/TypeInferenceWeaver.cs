using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Modification.Modlels;
using plamp.Alternative.Visitors.Base;

namespace plamp.Alternative.Visitors;

public class TypeInferenceWeaver : BaseExtendedWeaver<TypeInferenceContext, TypeInferenceInnerContext>
{
    protected override TypeInferenceInnerContext CreateInnerContext(TypeInferenceContext context)
    {
        return new TypeInferenceInnerContext(
            context.Symbol,
            context.Signature,
            context.FileName,
            context.Exceptions,
            new Stack<Dictionary<string, VariableDefinitionNode>>(),
            new Stack<Type?>());
    }

    protected override WeaveResult CreateWeaveResult(TypeInferenceInnerContext innerContext, TypeInferenceContext outerContext)
    {
        var res = base.CreateWeaveResult(innerContext, outerContext);
        return res with { Exceptions = innerContext.Exceptions };
    }

    protected override VisitResult VisitDef(DefNode node, TypeInferenceInnerContext context)
    {
        context.VariableTypeStack.Clear();
        context.TypeResolveStack.Clear();
        if (!context.Signature.MethodList.TryGetValue(node.Name.MemberName, out var method)) return VisitResult.SkipChildren;
        foreach (var parameter in node.ParameterList
                     .Select(x => x.Name)
                     .Zip(method.GetParameters()))
        {
            var dict = new Dictionary<string, VariableDefinitionNode>();
            context.VariableTypeStack.Push(dict);
            var argDef = new VariableDefinitionNode(
                new TypeNode(new MemberNode(parameter.Second.ParameterType.Name), [])
                    { Symbol = parameter.Second.ParameterType }, new MemberNode(parameter.First.MemberName));
            var argSymbol = context.Symbol.GetSymbol(parameter.First);
            context.Symbol.AddSymbol(argDef, argSymbol.Key, argSymbol.Value);
            dict.Add(parameter.First.MemberName, argDef);
        }

        VisitInternal(node.Body, context);
        
        context.VariableTypeStack.Clear();
        context.TypeResolveStack.Clear();
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context)
    {
        var variables = context.VariableTypeStack.Peek();
        if (node.Type != null && variables.TryGetValue(node.Member.MemberName, out var varDef))
        {
            var record = PlampNativeExceptionInfo.DuplicateVariableDefinition();
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(varDef, record, context.FileName));
        }
        else if (node.Type == null && variables.TryGetValue(node.Member.MemberName, out varDef))
        {
            var typeNode = new TypeNode(varDef.Type.TypeName, null) { Symbol = varDef.Type.Symbol };
            var newDef = new VariableDefinitionNode(typeNode, node.Member);
            context.Symbol.ReplaceSymbol(node, newDef);
            Replace(node, newDef);
        }
        else if (node.Type != null && !variables.TryGetValue(node.Member.MemberName, out varDef))
        {
            if (node.Type.Symbol == null)
            { 
                var type = TypeResolveHelper.ResolveType(node.Type, context.Exceptions, context.Symbol, context.FileName);
                if (type != null)
                {
                    var newType = new TypeNode(node.Type.TypeName, []) { Symbol = type };
                    var newDef = new VariableDefinitionNode(newType, node.Member);
                    Replace(node, newType);
                    context.Symbol.ReplaceSymbol(node, newDef);
                    node = newDef;
                }
            }
            variables.Add(node.Member.MemberName, node);
            
        }
        else
        {
            variables.Add(node.Member.MemberName, node);
        }
        context.TypeResolveStack.Push(node.Type?.Symbol);

        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitBody(BodyNode node, TypeInferenceInnerContext context)
    {
        var prevVars = context.VariableTypeStack.Peek().ToDictionary(x => x.Key, x => x.Value);
        context.VariableTypeStack.Push(prevVars);
        foreach (var expression in node.ExpressionList)
        {
            context.TypeResolveStack.Clear();
            VisitInternal(expression, context);
        }

        context.VariableTypeStack.Pop();
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitAssign(AssignNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Left, context);
        if (node.Left is VariableDefinitionNode leftDef)
        {
            VisitInternal(leftDef, context);
            var leftType = context.TypeResolveStack.Pop();
            VisitInternal(node.Right, context);
            var rightType = context.TypeResolveStack.Pop();
            CheckDefType(leftDef, leftType, rightType);
        }
        else if (node.Left is MemberNode leftMember)
        {
            VisitInternal(node.Right, context);
            var rightType = context.TypeResolveStack.Pop();
            if (!context.VariableTypeStack.Peek().TryGetValue(leftMember.MemberName, out var def))
            {
                TypeNode? type = null;
                if (rightType != null) type = new TypeNode(new MemberNode(rightType.Name), []) { Symbol = rightType };
                var newDef = new VariableDefinitionNode(type, leftMember);
                Replace(leftMember, newDef);
                context.Symbol.ReplaceSymbol(leftMember, newDef);
                context.VariableTypeStack.Peek().Add(newDef.Member.MemberName, newDef);
            }
            else
            {
                CheckDefType(def, def.Type.Symbol, rightType);
            }
        }

        return VisitResult.SkipChildren;

        void CheckDefType(VariableDefinitionNode leftDefinition, Type? leftType, Type? rightType)
        {
            if (leftType != rightType && leftType != null)
            {
                var record = PlampNativeExceptionInfo.CannotAssign();
                context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
            }
            else if (leftType == null && leftType != rightType)
            {
                var newType = new TypeNode(leftDefinition.Type?.TypeName, []) { Symbol = rightType };
                var newDef = new VariableDefinitionNode(newType, leftDefinition.Member);
                Replace(leftDefinition, newDef);
                context.Symbol.ReplaceSymbol(leftDefinition, newDef);
                context.VariableTypeStack.Peek().Add(newDef.Member.MemberName, newDef);
            }
            else
            {
                context.VariableTypeStack.Peek().Add(leftDefinition.Member.MemberName, leftDefinition);
            }
        }
    }

    protected override VisitResult VisitBinaryExpression(BaseBinaryNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Left, context);
        var leftType = context.TypeResolveStack.Pop();
        VisitInternal(node.Right, context);
        var rightType = context.TypeResolveStack.Pop();
        if (leftType != rightType && leftType != null && rightType != null)
        {
            var record = PlampNativeExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
            context.TypeResolveStack.Push(null);
            return VisitResult.SkipChildren;
        }

        if (leftType == rightType && leftType != null)
        {
            if ((node is AndNode or OrNode && !TypeResolveHelper.IsLogical(leftType))
                || (node is not LessNode and not LessOrEqualNode and not GreaterNode and not GreaterOrEqualNode
                    and not EqualNode and not NotEqualNode
                && !TypeResolveHelper.IsNumeric(leftType)))
            {
                var record = PlampNativeExceptionInfo.CannotApplyOperator();
                context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
                context.TypeResolveStack.Push(null);
                return VisitResult.SkipChildren;
            }
            context.TypeResolveStack.Push(leftType);
            return VisitResult.SkipChildren;
        }

        context.TypeResolveStack.Push(null);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, TypeInferenceInnerContext context)
    {
        VisitInternal(unaryNode.Inner, context);
        var innerType = context.TypeResolveStack.Pop();
        if (innerType == null)
        {
            context.TypeResolveStack.Push(null);
            return VisitResult.SkipChildren;
        }

        if ((TypeResolveHelper.IsLogical(innerType) && unaryNode is not NotNode) ||
            (!TypeResolveHelper.IsNumeric(innerType) && unaryNode is PrefixIncrementNode or PrefixDecrementNode or UnaryMinusNode))
        {
            var record = PlampNativeExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(unaryNode, record, context.FileName));
            context.TypeResolveStack.Push(null);
            return VisitResult.SkipChildren;
        }
        
        context.TypeResolveStack.Push(innerType);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitLiteral(LiteralNode literalNode, TypeInferenceInnerContext context)
    {
        context.TypeResolveStack.Push(literalNode.Type);
        return VisitResult.Continue;
    }

    protected override VisitResult VisitMember(MemberNode node, TypeInferenceInnerContext context)
    {
        if (!context.VariableTypeStack.Peek().TryGetValue(node.MemberName, out var value))
        {
            var record = PlampNativeExceptionInfo.CannotFindMember();
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
        }
        context.TypeResolveStack.Push(value?.Type.Symbol);
        return VisitResult.Continue;
    }

    protected override VisitResult VisitCall(CallNode node, TypeInferenceInnerContext context)
    {
        var name = node.MethodName.MemberName;
        if (!context.Signature.MethodList.TryGetValue(name, out var signature))
        {
            var record = PlampNativeExceptionInfo.UnknownFunction();
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
            context.TypeResolveStack.Push(null);
            foreach (var arg in node.Args)
            {
                VisitInternal(arg, context);
                context.TypeResolveStack.Pop();
            }
            return VisitResult.SkipChildren;
        }

        if (node.Args.Count != signature.GetParameters().Length)
        {
            var record = PlampNativeExceptionInfo.UnknownFunction();
            context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(node, record, context.FileName));
            context.TypeResolveStack.Push(null);
            foreach (var arg in node.Args)
            {
                VisitInternal(arg, context);
                context.TypeResolveStack.Pop();
            }
            return VisitResult.SkipChildren;
        }
        
        foreach (var (arg, parameter) in node.Args.Zip(signature.GetParameters()))
        {
            VisitInternal(arg, context);
            var argType = context.TypeResolveStack.Pop();
            if (argType != parameter.ParameterType)
            {
                var record = PlampNativeExceptionInfo.WrongParameterType();
                context.Exceptions.Add(context.Symbol.CreateExceptionForSymbol(arg, record, context.FileName));
            }
        }
        context.TypeResolveStack.Push(signature.ReturnType);
        return VisitResult.SkipChildren;
    }
}

public record TypeInferenceContext(
    SymbolTable Symbol,
    ModuleSignature Signature,
    string FileName,
    List<PlampException> Exceptions);

public record TypeInferenceInnerContext(
    SymbolTable Symbol,
    ModuleSignature Signature,
    string FileName,
    List<PlampException> Exceptions,
    Stack<Dictionary<string, VariableDefinitionNode>> VariableTypeStack,
    Stack<Type?> TypeResolveStack);