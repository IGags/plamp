using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Visitors.Base;

namespace plamp.Alternative.Visitors;

public class TypeInferenceWeaver : BaseExtendedWeaver<TypeInferenceContext, TypeInferenceInnerContext, TypeInferenceResult>
{
    protected override TypeInferenceInnerContext CreateInnerContext(TypeInferenceContext context)
    {
        return new TypeInferenceInnerContext(context.Symbols, context.ModuleSignatures, context.FileName,
            context.Exceptions, [], [], []);
    }

    protected override TypeInferenceResult CreateWeaveResult(TypeInferenceInnerContext innerContext, TypeInferenceContext outerContext)
    {
        return new TypeInferenceResult(innerContext.Exceptions);
    }

    protected override VisitResult VisitDef(DefNode node, TypeInferenceInnerContext context)
    {
        foreach (var parameterNode in node.ParameterList)
        {
            context.Arguments.Add(parameterNode.Name.MemberName, parameterNode);
        }

        VisitInternal(node, context);
        
        context.Arguments.Clear();
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitBody(BodyNode node, TypeInferenceInnerContext context)
    {
        //Not body nodes first to check outer scope definitions, but not change position of others
        foreach (var child in node
                     .Visit().Where(x => x is not BodyNode and not WhileNode and not ConditionNode)
                     .Concat(node.Visit().Where(x => x is BodyNode or WhileNode or ConditionNode)))
        {
            context.InnerExpressionType = null;
            VisitInternal(child, context);
            context.InnerExpressionType = null;
        }

        foreach (var variable in context.CurrentScopeDefinitions)
        {
            context.VariableDefinitions.Remove(variable);
        }
        context.CurrentScopeDefinitions.Clear();
        
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context)
    {
        if (context.Arguments.ContainsKey(node.Member.MemberName))
        {
            var record = PlampNativeExceptionInfo.ArgumentAlreadyDefined();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
            return VisitResult.SkipChildren;
        }

        if (context.VariableDefinitions.TryGetValue(node.Member.MemberName, out _))
        {
            var record = PlampNativeExceptionInfo.DuplicateVariableDefinition();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
            return VisitResult.SkipChildren;
        }

        var variableType = TypeResolveHelper.ResolveType(node.Type, context.Exceptions, context.Symbols, context.FileName);
        if (variableType != null)
        {
            var newTypeNode = new TypeNode(node.Type.TypeName, []) { Symbol = variableType };
            ReplaceChild(node, node.Type, newTypeNode, context);
            context.InnerExpressionType = variableType;
        }
        
        context.CurrentScopeDefinitions.Add(node.Member.MemberName);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, TypeInferenceInnerContext context)
    {
        VisitInternal(unaryNode.Inner, context);
        if (context.InnerExpressionType == typeof(bool)
            && unaryNode is not NotNode)
        {
            var record = PlampNativeExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(unaryNode, record, context.FileName));
            context.InnerExpressionType = null;
        }
        else if (context.InnerExpressionType != typeof(bool) && unaryNode is NotNode)
        {
            var record = PlampNativeExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(unaryNode, record, context.FileName));
            context.InnerExpressionType = typeof(bool);
        }
        else if (context.InnerExpressionType != null && !IsNumeric(context.InnerExpressionType) &&
                unaryNode is PrefixIncrementNode or PrefixDecrementNode or PostfixDecrementNode or PostfixIncrementNode or UnaryMinusNode)
        {
            var record = PlampNativeExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(unaryNode, record, context.FileName));
            context.InnerExpressionType = null;
        }
        else
        {
            context.InnerExpressionType = null;
        }

        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitBinaryExpression(BaseBinaryNode node, TypeInferenceInnerContext context)
    {
        if (node is BaseAssignNode)
        {
            return base.VisitBinaryExpression(node, context);
        }
        
        VisitInternal(node.Left, context);
        var leftType = context.InnerExpressionType;
        VisitInternal(node.Right, context);
        var rightType = context.InnerExpressionType;
        context.InnerExpressionType = null;

        if (leftType == null && rightType == null) return VisitResult.SkipChildren;
        
        if (IsArithmetic(node))
        {
            if ((leftType != null && !IsNumeric(leftType)) || (rightType != null && !IsNumeric(rightType))
                || leftType != rightType)
            {
                var record = PlampNativeExceptionInfo.CannotApplyOperator();
                context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
                context.InnerExpressionType = null;
                return VisitResult.SkipChildren;
            }

            context.InnerExpressionType = leftType ?? rightType;
        }
        else if (IsComparisionNode(node))
        {
            if (leftType != null && rightType != null && leftType != rightType)
            {
                var record = PlampNativeExceptionInfo.CannotApplyOperator();
                context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
                context.InnerExpressionType = null;
                return VisitResult.SkipChildren;
            }
            
            if((leftType != null && !IsNumeric(leftType)) || (rightType != null && !IsNumeric(rightType)
               && node is not EqualNode and not NotEqualNode))
            {
                var record = PlampNativeExceptionInfo.CannotApplyOperator();
                context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
                context.InnerExpressionType = null;
                return VisitResult.SkipChildren;
            }
            
            context.InnerExpressionType = leftType ?? rightType;
        }
        else if(IsBinaryLogicGate(node))
        {
            if ((leftType != null && leftType != typeof(bool)) || (rightType != null && rightType != typeof(bool)))
            {
                var record = PlampNativeExceptionInfo.CannotApplyOperator();
                context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
            }

            context.InnerExpressionType = typeof(bool);
        }

        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitCall(CallNode node, TypeInferenceInnerContext context)
    {
        if (!context.ModuleSignatures.TryGetValue(node.MethodName.MemberName, out var def))
        {
            AddUnexpectedCallExceptionAndValidateChildren();
            context.InnerExpressionType = null;
            return VisitResult.SkipChildren;
        }

        context.InnerExpressionType = def.ReturnType.Symbol;

        if (node.Args.Count != def.ParameterList.Count)
        {
            AddUnexpectedCallExceptionAndValidateChildren();
            return VisitResult.SkipChildren;
        }

        var invalid = false;
        foreach (var (arg, defType) in node.Args.Zip(def.ParameterList.Select(x => x.Type)))
        {
            VisitInternal(arg, context);
            var argType = context.InnerExpressionType;
            if (argType != defType.Symbol)
            {
                invalid = true;
            }
        }

        if (invalid)
        {
            var record = PlampNativeExceptionInfo.UnknownFunction();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
        }

        return VisitResult.SkipChildren;
        
        void AddUnexpectedCallExceptionAndValidateChildren()
        {
            var record = PlampNativeExceptionInfo.UnknownFunction();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
            foreach (var parameter in node.Args)
            {
                VisitInternal(parameter, context);
                context.InnerExpressionType = null;
            }
        }
    }

    protected override VisitResult VisitLiteral(LiteralNode literalNode, TypeInferenceInnerContext context)
    {
        context.InnerExpressionType = literalNode.Type;
        return VisitResult.Continue;
    }

    protected override VisitResult VisitMember(MemberNode node, TypeInferenceInnerContext context)
    {
        if (!context.VariableDefinitions.TryGetValue(node.MemberName, out var variable))
        {
            var record = PlampNativeExceptionInfo.CannotFindMember();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
            context.InnerExpressionType = null;
            return VisitResult.SkipChildren;
        }
        
        context.InnerExpressionType = variable.Type.Symbol;
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitAssign(AssignNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Right, context);
        var rightType = context.InnerExpressionType;
        context.InnerExpressionType = null;
        if (node.Left is MemberNode leftMember)
        {
            if (!context.VariableDefinitions.TryGetValue(leftMember.MemberName, out var variable))
            {
                TypeNode? typeNode = null;
                if (rightType != null)
                {
                    var memberSymbol = context.Symbols.GetSymbol(leftMember)!.Value;
                    var typeName = new MemberNode(rightType.Name);
                    context.Symbols.AddSymbol(typeName, memberSymbol.Key, memberSymbol.Value);
                    typeNode = new TypeNode(typeName, []) { Symbol = rightType };
                    context.Symbols.AddSymbol(typeNode, memberSymbol.Key, memberSymbol.Value);
                }

                var definition = new VariableDefinitionNode(typeNode, leftMember);
                ReplaceChild(node, leftMember, definition, context);
                context.CurrentScopeDefinitions.Add(leftMember.MemberName);
                context.VariableDefinitions.Add(leftMember.MemberName, definition);
                return VisitResult.SkipChildren;
            }

            if (variable.Type.Symbol != null && rightType != null && variable.Type.Symbol != rightType)
            {
                var record = PlampNativeExceptionInfo.CannotAssign();
                context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
                return VisitResult.SkipChildren;
            }
        }
        else if (node.Left is VariableDefinitionNode leftDef)
        {
            VisitVariableDefinition(leftDef, context);
            var leftType = context.InnerExpressionType;
            if (leftType != null && rightType != null && leftType != rightType)
            {
                var record = PlampNativeExceptionInfo.CannotAssign();
                context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
            }
        }
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitWhile(WhileNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Condition, context);
        var predicateType = context.InnerExpressionType;
        if (predicateType != null && predicateType != typeof(bool))
        {
            var record = PlampNativeExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
        }

        VisitInternal(node.Body, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitCondition(ConditionNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Predicate, context);
        var predicateType = context.InnerExpressionType;
        if (predicateType != null && predicateType != typeof(bool))
        {
            var record = PlampNativeExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.Symbols.CreateExceptionForSymbol(node, record, context.FileName));
        }

        VisitInternal(node.IfClause, context);
        if (node.ElseClause != null) VisitInternal(node.ElseClause, context);
        return VisitResult.SkipChildren;
    }

    private void ReplaceChild(NodeBase node, NodeBase oldChild, NodeBase newChild, TypeInferenceInnerContext context)
    {
        context.Symbols.ReplaceSymbol(oldChild, newChild);
        node.ReplaceChild(oldChild, newChild);
    }

    private bool IsNumeric(Type type) => type == typeof(int) || type == typeof(uint) || type == typeof(long) ||
                                         type == typeof(ulong) || type == typeof(byte) || type == typeof(float) ||
                                         type == typeof(double);

    private bool IsArithmetic(BaseBinaryNode baseBinary) =>
        baseBinary is PlusNode or MinusNode or MultiplyNode or DivideNode or ModuloNode;

    private bool IsBinaryLogicGate(BaseBinaryNode baseBinary) => baseBinary is OrNode or AndNode;

    private bool IsComparisionNode(BaseBinaryNode baseBinary) => baseBinary is EqualNode or NotEqualNode or LessNode
        or LessOrEqualNode or GreaterNode or GreaterOrEqualNode;
}

public record TypeInferenceContext(
    SymbolTable Symbols,
    string FileName,
    Dictionary<string, DefNode> ModuleSignatures,
    List<PlampException> Exceptions);

public record TypeInferenceInnerContext(
    SymbolTable Symbols,
    Dictionary<string, DefNode> ModuleSignatures,
    string FileName,
    List<PlampException> Exceptions,
    Dictionary<string, VariableDefinitionNode> VariableDefinitions,
    Dictionary<string, ParameterNode> Arguments,
    List<string> CurrentScopeDefinitions)
{
    public Type? InnerExpressionType { get; set; }
}
    
public record TypeInferenceResult(List<PlampException> Exceptions);