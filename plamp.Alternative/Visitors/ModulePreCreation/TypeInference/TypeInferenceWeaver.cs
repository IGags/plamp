using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public class TypeInferenceWeaver : BaseWeaver<PreCreationContext, TypeInferenceInnerContext>
{
    protected override VisitorGuard Guard => VisitorGuard.FuncDefWithBody;

    #region TopLevel

    protected override VisitResult PreVisitFunction(FuncNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.CurrentFunc = node;
        var nonDupArgs = node.ParameterList
            .GroupBy(x => x.Name.Value)
            .Where(x => x.Count() == 1)
            .SelectMany(x => x);
        
        foreach (var arg in nonDupArgs) context.Arguments.Add(arg.Name.Value, arg.Type.TypeInfo);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitFunction(FuncNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.Arguments.Clear();
        context.CurrentFunc = null;
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitBody(BodyNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.EnterBody();
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitInstruction(NodeBase node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.NextInstruction();
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitBody(BodyNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.ExitBody();
        return VisitResult.Continue;
    }

    #endregion

    #region Variable definition

    protected override VisitResult PreVisitVariableName(VariableNameNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var exception = false;
        if (context.Arguments.ContainsKey(node.Value))
        {
            var record = PlampExceptionInfo.ArgumentAlreadyDefined();
            SetExceptionToSymbol(node, record, context);
            exception = true;
        }
        
        if (context.TryGetVariable(node.Value, out var other))
        {
            var record = PlampExceptionInfo.DuplicateVariableDefinition();
            SetExceptionToSymbol(node, record, context);
            SetExceptionToSymbol(other, record, context);
            exception = true;
        }

        if (!exception && parent is VariableDefinitionNode def)
        {
            context.TryAddVariable(def);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var type = node.Type?.TypeInfo;
        if (type == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        context.InnerExpressionTypeStack.Push(type);
        if (parent is AssignNode) return VisitResult.Continue;
        
        var assign = WeaveAssignmentDefault(node);
        if(assign != null) Replace(node, _ => assign, context);
        return VisitResult.Continue;
    }

    #endregion

    #region n-ary operators

    protected override VisitResult PostVisitUnary(BaseUnaryNode unaryNode, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var type = context.InnerExpressionTypeStack.Pop();
        if (type == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }

        if (SymbolSearchUtility.IsLogical(type) && unaryNode is NotNode)
        {
            context.InnerExpressionTypeStack.Push(Builtins.Bool);
            return VisitResult.Continue;
        }
        
        if (SymbolSearchUtility.IsNumeric(type)
            && unaryNode is PrefixIncrementNode 
                or PrefixDecrementNode 
                or PostfixDecrementNode 
                or PostfixIncrementNode
                or UnaryMinusNode)
        {
            context.InnerExpressionTypeStack.Push(type);
            return VisitResult.Continue;
        }
        
        
        var record = PlampExceptionInfo.CannotApplyOperator();
        SetExceptionToSymbol(unaryNode, record, context);
        context.InnerExpressionTypeStack.Push(null);
        return VisitResult.Continue;
    }
    
    protected override VisitResult PostVisitBinary(BaseBinaryNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var rightType = context.InnerExpressionTypeStack.Pop();
        var leftType = context.InnerExpressionTypeStack.Pop();

        if (leftType == null && rightType == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        
        if (Arithmetic(node)) return ValidateBinaryArithmetic(node, leftType, rightType, context);
        if (ComparisionNode(node)) return ValidateBinaryComparision(node, leftType, rightType, context);
        if (BinaryLogicGate(node)) return ValidateBinaryLogical(node, leftType, rightType, context);
        throw new ArgumentException("Parser error, unexpected binary operator type");
    }

    #endregion
    
    #region BinaryValidation

    private VisitResult ValidateBinaryLogical(
        BaseBinaryNode node,
        ITypeInfo? leftType,
        ITypeInfo? rightType,
        TypeInferenceInnerContext context)
    {
        if (   (leftType  != null && !SymbolSearchUtility.IsLogical(leftType)) 
            || (rightType != null && !SymbolSearchUtility.IsLogical(rightType)))
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            SetExceptionToSymbol(node, record, context);
        }

        context.InnerExpressionTypeStack.Push(Builtins.Bool);
        return VisitResult.Continue;
    }
    
    private VisitResult ValidateBinaryComparision(
        BaseBinaryNode node,
        ITypeInfo? leftType, 
        ITypeInfo? rightType, 
        TypeInferenceInnerContext context)
    {
        context.InnerExpressionTypeStack.Push(Builtins.Bool);
        if (leftType != null
            && rightType != null
            && SymbolSearchUtility.IsLogical(leftType)
            && SymbolSearchUtility.IsLogical(rightType)
            && node is NotEqualNode or EqualNode)
        {
            return VisitResult.Continue;
        }

        if (   leftType  != null 
            && rightType != null 
            && SymbolSearchUtility.IsNumeric(leftType) 
            && SymbolSearchUtility.IsNumeric(rightType))
        {
            if (leftType.Equals(rightType) || TryExpandNumericBinary(node.Left, node.Right, leftType, rightType, context, out _))
            {
                return VisitResult.Continue;
            }
        }
        
        var record = PlampExceptionInfo.CannotApplyOperator();
        SetExceptionToSymbol(node, record, context);
        return VisitResult.Continue;
    }
    
    private VisitResult ValidateBinaryArithmetic(
        BaseBinaryNode node,
        ITypeInfo? leftType, 
        ITypeInfo? rightType, 
        TypeInferenceInnerContext context)
    {
        if (TryValidateStringAddition(node, leftType, rightType, context, out var result, out var resultType))
        {
            context.InnerExpressionTypeStack.Push(resultType);
            return result.Value;
        }

        if (TryValidateNumericBinary(node, leftType, rightType, context, out result, out resultType))
        {
            context.InnerExpressionTypeStack.Push(resultType);
            return result.Value;
        }
        
        var record = PlampExceptionInfo.CannotApplyOperator();
        SetExceptionToSymbol(node, record, context);
        context.InnerExpressionTypeStack.Push(null);
        return VisitResult.Continue;
    }
    
    private bool TryValidateStringAddition(
        BaseBinaryNode node,
        ITypeInfo? leftType, 
        ITypeInfo? rightType, 
        TypeInferenceInnerContext context,
        [NotNullWhen(true)]out VisitResult? result,
        out ITypeInfo? resultType)
    {
        result = null;
        resultType = null;
        if (node is not AddNode addition
            || (leftType == null  && rightType == null)
            || (leftType != null  && !SymbolSearchUtility.IsString(leftType))
            || (rightType != null && !SymbolSearchUtility.IsString(rightType)))
        {
            return false;
        }
        
        result = VisitResult.Continue;
        resultType = Builtins.String;
        var callName = new FuncCallNameNode(nameof(Builtins.StrConcat.Name));
        
        if (!context.TranslationTable.TryGetSymbol(node, out var position))
        {
            throw new Exception("Compiler exception: symbol position is not set");
        }
        
        context.TranslationTable.AddSymbol(callName, position);
        Replace(addition, ReplaceToConcat, context);
        return true;

        NodeBase ReplaceToConcat(AddNode addNode)
        {
            if (addNode is { Left: LiteralNode {Value: not null} leftLiteral, Right: LiteralNode {Value: not null} rightLiteral })
            {
                return new LiteralNode(leftLiteral.Value.ToString() + rightLiteral.Value, Builtins.String);
            }
            
            var concatCall = new CallNode(null, callName, [addition.Left, addition.Right], []) { FnInfo = Builtins.StrConcat };
            return concatCall;
        }
    }

    private bool TryValidateNumericBinary(
        BaseBinaryNode node,
        ITypeInfo? leftType, 
        ITypeInfo? rightType, 
        TypeInferenceInnerContext context,
        [NotNullWhen(true)]out VisitResult? result,
        out ITypeInfo? resultType)
    {
        result = null;
        resultType = null;
        if (   (leftType  != null && !SymbolSearchUtility.IsNumeric(leftType))
            || (rightType != null && !SymbolSearchUtility.IsNumeric(rightType))
            || (leftType == null && rightType == null))
        {
            return false;
        }

        result = VisitResult.Continue;
        if (leftType != null 
            && SymbolSearchUtility.IsNumeric(leftType) 
            && rightType != null 
            && SymbolSearchUtility.IsNumeric(rightType) 
            && !leftType.Equals(rightType))
        {
            return TryExpandNumericBinary(node.Left, node.Right, leftType, rightType, context, out resultType);
        }
        
        var innerType = leftType == null && rightType == null ? null : leftType ?? rightType!;
        resultType = innerType;
        return true;
    }
    
    #endregion

    #region TypeExpansion

    private bool TryExpandType(
        NodeBase from, 
        ITypeInfo fromType,
        ITypeInfo toType, 
        TypeInferenceInnerContext context)
    {
        if (!SymbolSearchUtility.ImplicitlyConvertable(fromType, toType)) return false;
        if (!SymbolSearchUtility.NeedToCast(fromType, toType)) return true;
        var toTypeNode = new TypeNode(new TypeNameNode(toType.Name));
        context.TranslationTable.AddSymbol(toTypeNode, default);
        toTypeNode.TypeInfo = toType;
        var expanded = new CastNode(toTypeNode, from) { FromType = fromType };
        Replace(from, _ => expanded, context);
        return true;
    }

    private bool TryExpandNumericBinary(
        NodeBase left,
        NodeBase right,
        ITypeInfo leftType,
        ITypeInfo rightType,
        TypeInferenceInnerContext context,
        [NotNullWhen(true)] out ITypeInfo? resultType)
    {
        resultType = leftType;
        if (leftType.Equals(rightType)) return true;
        
        if (TryExpandType(left, leftType, rightType, context))
        {
            resultType = rightType;
            return true;
        }

        if (TryExpandType(right, rightType, leftType, context))
        {
            resultType = leftType;
            return true;
        }

        var intType = Builtins.Int;
        if (TryExpandBoth(left, right, leftType, rightType, intType, context))
        {
            resultType = intType;
            return true;
        }

        var longType = Builtins.Long;
        if (TryExpandBoth(left, right, leftType, rightType, longType, context))
        {
            resultType = longType;
            return true;
        }
        
        resultType = null;
        return false;
    }

    private bool TryExpandBoth(NodeBase left,
        NodeBase right,
        ITypeInfo leftType,
        ITypeInfo rightType,
        ITypeInfo target,
        TypeInferenceInnerContext context)
    {
        if (SymbolSearchUtility.ImplicitlyConvertable(leftType, target)
            && SymbolSearchUtility.ImplicitlyConvertable(rightType, target))
        {
            TryExpandType(left, leftType, target, context);
            TryExpandType(right, rightType, target, context);
            return true;
        }

        return false;
    }

    #endregion
    
    #region Arrays

    protected override VisitResult PostVisitInitArray(InitArrayNode node, TypeInferenceInnerContext context,
        NodeBase? parent)
    {
        var lengthType = context.InnerExpressionTypeStack.Pop();
        if (lengthType == null)
        {
            SetExceptionToSymbol(node, PlampExceptionInfo.ArrayInitializationMustHasLength(), context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }

        if (!TryExpandType(node.LengthDefinition, lengthType, Builtins.Int, context))
        {
            SetExceptionToSymbol(node.LengthDefinition, PlampExceptionInfo.ArrayLengthMustBeInteger(), context);
        }

        ITypeInfo? arrType = null;
        if (node.ArrayItemType.TypeInfo != null)
        {
            arrType = node.ArrayItemType.TypeInfo.MakeArrayType();
        }

        context.InnerExpressionTypeStack.Push(arrType);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitArrayIndexer(IndexerNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var indexationTargetType = context.InnerExpressionTypeStack.Pop();
        var indexerExpression = context.InnerExpressionTypeStack.Pop();

        if (indexationTargetType == null || indexerExpression == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }

        if (!TryExpandType(node.IndexMember, indexerExpression, Builtins.Int, context))
        {
            SetExceptionToSymbol(node.IndexMember, PlampExceptionInfo.IndexerValueMustBeInteger(), context);
        }

        if (indexationTargetType.ElementType() == null)
        {
            SetExceptionToSymbol(node.From, PlampExceptionInfo.IndexerIsNotApplicable(), context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        var elementType = indexationTargetType.ElementType();
        node.ItemType = elementType;
        context.InnerExpressionTypeStack.Push(elementType);
        return VisitResult.Continue;
    }

    #endregion

    #region Func call

    protected override VisitResult PreVisitCall(CallNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.SaveInferenceStackSize();
        return VisitResult.Continue;
    }
    
    protected override VisitResult PostVisitCall(CallNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var argCount = context.InnerExpressionTypeStack.Count - context.RestoreInferenceStackSize();
        if (argCount < 0) throw new InvalidOperationException("Parser error, expression type stack invalid");
        var argTypes = new List<ITypeInfo?>();
        for (var i = 0; i < argCount; i++)
        {
            argTypes.Add(context.InnerExpressionTypeStack.Pop());
        }
        argTypes.Reverse();

        var errRecord = SymbolSearchUtility.TryGetFuncOrErrorRecord(node.Name.Value, context.Dependencies, out var fnRef);
        if (errRecord != null)
        {
            SetExceptionToSymbol(node, errRecord, context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }
        
        ArgumentNullException.ThrowIfNull(fnRef);
        var fnParams = fnRef.Arguments;
        //Если то, что нашлось, содержит другое число аргументов в объявлении.
        if (fnParams.Count != argTypes.Count)
        {
            var record = PlampExceptionInfo.FunctionHasDifferentArgCount(fnParams.Count, argTypes.Count);
            SetExceptionToSymbol(node, record, context);
            
            if (fnRef.ReturnType.IsGenericType || fnRef.ReturnType.IsGenericTypeParameter)
            {
                context.InnerExpressionTypeStack.Push(null);
            }
            else
            {
                context.InnerExpressionTypeStack.Push(fnRef.ReturnType);
            }
            
            return VisitResult.SkipChildren;
        }

        if (fnRef.IsGenericFuncDefinition)
        {
            fnRef = node.GenericArguments.Any() 
                ? InferenceExplicitGenericFuncCall(node, fnRef, context) 
                : InferenceImplicitGenericFuncCall(node, fnRef, argTypes, context);
        }
        
        if (fnRef == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        for (var i = 0; i < fnRef.Arguments.Count; i++)
        {
            var actualType = argTypes[i];
            if(actualType == null) continue;
                
            var expectedType = fnRef.Arguments[i].Type;
                
            if(SymbolSearchUtility.ImplicitlyConvertable(actualType, expectedType)) continue;
                
            var record = PlampExceptionInfo.CannotApplyArgument();
            SetExceptionToSymbol(node.Args[i], record, context);
        }

        if (fnRef.IsGenericFuncDefinition)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        if(argTypes.Any(x => x == null))
        {
            context.InnerExpressionTypeStack.Push(fnRef.ReturnType);
            return VisitResult.Continue;
        }
        
        for (var i = 0; i < argTypes.Count; i++)
        {
            var argType = argTypes[i];
            ArgumentNullException.ThrowIfNull(argType);
            //Так как сигнатура была определена, то правильный каст типов гарантирован.
            TryExpandType(node.Args[i], argType, fnRef.Arguments[i].Type, context);
        }
        
        node.FnInfo = fnRef;
        context.InnerExpressionTypeStack.Push(fnRef.ReturnType);
        return VisitResult.Continue;
    }

    private IFnInfo? InferenceExplicitGenericFuncCall(
        CallNode node,
        IFnInfo definitionFn,
        TypeInferenceInnerContext context)
    {
        var genericArguments = node.GenericArguments;
        var expectedCt = definitionFn.GetGenericParameters().Count; 
        if (genericArguments.Count != expectedCt)
        {
            var record = PlampExceptionInfo.GenericFuncDefinitionHasDifferentParameterCount(expectedCt, genericArguments.Count);
            SetExceptionToSymbol(node, record, context);
            return null;
        }

        var genericArgTypes = node.GenericArguments.Select(x => x.TypeInfo).ToList();
        var notNullGenerics = genericArgTypes.OfType<ITypeInfo>().ToList();
        if (notNullGenerics.Count != expectedCt) return null;

        var fnImpl = definitionFn.MakeGenericFunc(notNullGenerics);
        return fnImpl;
    }
    
    private IFnInfo? InferenceImplicitGenericFuncCall(
        CallNode node,
        IFnInfo definitionFn,
        IReadOnlyList<ITypeInfo?> argTypes,
        TypeInferenceInnerContext context)
    {
        var fnParams = definitionFn.Arguments;
        
        //Проверяем на соответствие типов аргументов типам параметров(аргументы - то с чем используют, параметры - то, что написано в объявлении)
        var genericMapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        for (var i = 0; i < argTypes.Count; i++)
        {
            var argType = argTypes[i];
            var parameterType = fnParams[i].Type;
            
            if (argType == null) continue;
            SymbolSearchUtility.FillGenericMapping(parameterType, argType, genericMapping);
        }

        var parameterGrouping = genericMapping.GroupBy(x => x.Key, x => x.Value);

        var deduplicated = parameterGrouping.ToDictionary(x => x.Key, x => x.ToHashSet());
        
        var correctGenericMapping = new Dictionary<ITypeInfo, ITypeInfo>();
        foreach (var group in deduplicated)
        {
            if (group.Value.Count > 1)
            {
                var implementationNames = group.Value.Select(x => x.Name);
                var record = PlampExceptionInfo.GenericFunctionParameterCannotHasManyImplementations(group.Key.Name, implementationNames);
                SetExceptionToSymbol(node, record, context);
                continue;
            }

            var paramType = group.Value.Single();
            correctGenericMapping.Add(group.Key, paramType);
        }

        var invalid = false;
        var orderedArgs = new List<ITypeInfo>();
        foreach (var genericParam in definitionFn.GetGenericParameters())
        {
            if(correctGenericMapping.TryGetValue(genericParam, out var info))
            {
                orderedArgs.Add(info);
                continue;
            }
            var record = PlampExceptionInfo.GenericParameterHasNoImplementationType(genericParam.Name);
            SetExceptionToSymbol(node, record, context);
            invalid = true;
        }

        return invalid || !correctGenericMapping.Any() ? definitionFn : definitionFn.MakeGenericFunc(orderedArgs);
    }

    #endregion

    #region Looping

    protected override VisitResult PreVisitWhile(WhileNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        VisitNodeBase(node.Condition, context, node);
        var predicateType = context.InnerExpressionTypeStack.Pop();
        if (predicateType != null && !SymbolSearchUtility.IsLogical(predicateType))
        {
            var record = PlampExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.TranslationTable.SetExceptionToNode(node, record));
        }

        VisitNodeBase(node.Body, context, node);
        return VisitResult.SkipChildren;
    }

    #endregion

    #region Conditional

    protected override VisitResult PreVisitCondition(ConditionNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        VisitNodeBase(node.Predicate, context, node);
        var predicateType = context.InnerExpressionTypeStack.Pop();
        if (predicateType != null && !SymbolSearchUtility.IsLogical(predicateType))
        {
            var record = PlampExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.TranslationTable.SetExceptionToNode(node, record));
        }

        VisitNodeBase(node.IfClause, context, node);
        if (node.ElseClause != null) VisitNodeBase(node.ElseClause, context, node);
        return VisitResult.SkipChildren;
    }

    #endregion

    #region Assignment

    protected override VisitResult PreVisitAssign(AssignNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.SaveInferenceStackSize();
        return VisitResult.Continue;
    }

    private record AssignmentPair(ITypeInfo? SourceType, ITypeInfo? TargetType, NodeBase SourceNode, NodeBase TargetNode);
    
    protected override VisitResult PostVisitAssign(AssignNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var childrenCount = node.Sources.Count + node.Targets.Count;
        if (childrenCount == 0)
        {
            var record = PlampExceptionInfo.EmptyAssign();
            SetExceptionToSymbol(node, record, context);
            return VisitResult.Continue;
        }
        
        var assignTypeCount = context.InnerExpressionTypeStack.Count - context.RestoreInferenceStackSize();
        if (assignTypeCount == 0) return VisitResult.Continue;

        var types = new List<ITypeInfo?>(assignTypeCount);
        for (var i = 0; i < assignTypeCount; i++)
        {
            types.Add(context.InnerExpressionTypeStack.Pop());
        }
        
        if(childrenCount != types.Count) throw new Exception("Incorrect visitor code.");

        if (childrenCount % 2 != 0)
        {
            var record = PlampExceptionInfo.AssignSourceAndTargetCountMismatch();
            SetExceptionToSymbol(node, record, context);
            return VisitResult.Continue;
        }

        var assignmentPairs = new List<AssignmentPair>();
        for (var i = 0; i < types.Count / 2; i++)
        {
            var sourceIndex = types.Count - (i + 1);
            var pair = new AssignmentPair(types[sourceIndex], types[i], node.Sources[i], node.Targets[i]);
            assignmentPairs.Add(pair);
        }

        foreach (var assignment in assignmentPairs) 
        {
            if (assignment.SourceType is not null && assignment.SourceType.Equals(Builtins.Void))
            {
                var record = PlampExceptionInfo.CannotAssignNone();
                context.Exceptions.Add(context.TranslationTable.SetExceptionToNode(node, record));
            }

            switch (assignment.TargetNode)
            {
                case VariableDefinitionNode:
                case IndexerNode:
                case FieldAccessNode:
                    ValidateAssignmentTypeCompatibility(node, assignment.SourceNode, assignment.TargetType, context, assignment.SourceType);
                    continue;
                case MemberNode leftMember:
                    //Валидация того, что в параметр нельзя присвоить что-то иного типа.
                    if (context.Arguments.TryGetValue(leftMember.MemberName, out var argType))
                    {
                        ValidateAssignmentTypeCompatibility(node, assignment.SourceNode, argType, context, assignment.SourceType);
                        continue;
                    }
                    if (!context.TryGetVariable(leftMember.MemberName, out var variable))
                    {
                        CreateVariableDefinitionFromMember(leftMember, context, assignment.SourceType);
                        continue;
                    }
                    
                    ValidateAssignmentTypeCompatibility(node, assignment.SourceNode, variable.Type?.TypeInfo, context, assignment.SourceType);
                    continue;
                default: throw new Exception("Parser exception, invalid ast");
            }
        }
        
        return VisitResult.Continue;
    }
    
    private void ValidateAssignmentTypeCompatibility(
        NodeBase assignNode,
        NodeBase assignmentSource, 
        ITypeInfo? leftType, 
        TypeInferenceInnerContext context,
        ITypeInfo? rightType)
    {
        if (leftType == null || rightType == null || leftType.Equals(rightType)) return;
        if (TryExpandType(assignmentSource, rightType, leftType, context)) return;
        var record = PlampExceptionInfo.CannotAssign();
        SetExceptionToSymbol(assignNode, record, context);
    }

    private void CreateVariableDefinitionFromMember(MemberNode leftMember, TypeInferenceInnerContext context, ITypeInfo? rightType)
    {
        if (!context.TranslationTable.TryGetSymbol(leftMember, out var symbol))
            throw new ArgumentException("Parser error, symbol should exist");
        TypeNode? typeNode = null;
        if (rightType != null)
        {
            var typeName = new TypeNameNode(rightType.Name);
            context.TranslationTable.AddSymbol(typeName, symbol);
            typeNode = new TypeNode(typeName) { TypeInfo = rightType };
            context.TranslationTable.AddSymbol(typeNode, symbol);
        }

        var variableName = new VariableNameNode(leftMember.MemberName);
        context.TranslationTable.AddSymbol(variableName, symbol);
        var variableNode = new VariableDefinitionNode(typeNode, variableName);
        Replace(leftMember, _ => variableNode, context);
        if (context.TryAddVariable(variableNode)) return;
        var record = PlampExceptionInfo.DuplicateVariableDefinition();
        SetExceptionToSymbol(variableNode, record, context);
        if (context.TryGetVariable(variableName.Value, out var existing))
        {
            SetExceptionToSymbol(existing, record, context);
        }
        else
        {
            throw new Exception("Программа попыталась создать переменную, но нашла дубликат. При попытке извлечь дубликат программа не смогла его получить");
        }
    }

    private AssignNode? WeaveAssignmentDefault(VariableDefinitionNode variableDefinition)
    {
        if (variableDefinition.Type is null) throw new Exception("Syntax analyzer exception");
        var type = variableDefinition.Type.TypeInfo;

        if (type is null) return null;
        if (type.IsGenericTypeParameter) return null;

        var arrayUnderlyingType = type.ElementType();
        if (arrayUnderlyingType != null)
        {
            var itemType = new TypeNode(new TypeNameNode(arrayUnderlyingType.Name)) { TypeInfo = arrayUnderlyingType };
            var initArrayNode = new InitArrayNode(itemType, new LiteralNode(0, Builtins.Int));
            // []int a; => []int a := Array.Empty<int>()
            return new AssignNode([variableDefinition], [initArrayNode]);
        }

        if (SymbolSearchUtility.IsNumeric(type))
        {
            return new AssignNode([variableDefinition], [new LiteralNode(0, type)]);
        }

        if (SymbolSearchUtility.IsLogical(type))
        {
            return new AssignNode([variableDefinition], [new LiteralNode(false, type)]);
        }

        if (type.Equals(Builtins.Char))
        {
            return new AssignNode([variableDefinition], [new LiteralNode((char)0, type)]);
        }

        if (type.Equals(Builtins.String))
        {
            return new AssignNode([variableDefinition], [new LiteralNode(string.Empty, type)]);
        }

        var typeNode = new TypeNode(new TypeNameNode(type.Name)){ TypeInfo = type };
        return new AssignNode([variableDefinition], [new InitTypeNode(typeNode, [])]);
    }
    
    #endregion

    #region User-defined types

    protected override VisitResult PostVisitFieldAccess(FieldAccessNode accessNode, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var fromType = context.InnerExpressionTypeStack.Pop();
        if (fromType == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }

        var fieldName = accessNode.Field.Name;
        var fieldInfo = fromType.Fields.FirstOrDefault(x => x.Name == fieldName);
        if (fieldInfo == null)
        {
            var record = PlampExceptionInfo.FieldIsNotFound();
            SetExceptionToSymbol(accessNode.Field, record, context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }

        accessNode.Field.FieldInfo = fieldInfo;
        context.InnerExpressionTypeStack.Push(fieldInfo.FieldType);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitInitType(InitTypeNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        if (Builtins.SymTable.ModuleName.Equals(node.Type.TypeInfo?.ModuleName))
        {
            var error = PlampExceptionInfo.CannotInitBuiltinType();
            SetExceptionToSymbol(node, error, context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }
        
        context.InnerExpressionTypeStack.Push(node.Type.TypeInfo);
        return VisitResult.SkipChildren;
    }

    #endregion
    
    #region Misc
    
    protected override VisitResult PostVisitType(TypeNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        if(node.TypeInfo != null) return VisitResult.Continue;
        ITypeInfo? typeRef = null;

        if (node.GenericParameters.Count == 0 && context.CurrentFunc != null)
        {
            var currentModule = context.Dependencies.OfType<ISymTableBuilder>().FirstOrDefault();
            if (currentModule != null && currentModule.TryGetInfo(context.CurrentFunc.FuncName.Value, out IFnBuilderInfo? info))
            {
                var generics = info.GetGenericParameters();
                var type = generics.FirstOrDefault(x => x.Name.Equals(node.TypeName.Name));
                if (type != null) typeRef = type;
            }
        }

        if (typeRef == null)
        {
            var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(node, context.Dependencies, out typeRef);
            
            if (record != null)
            {
                SetExceptionToSymbol(node, record, context);
                return VisitResult.SkipChildren;
            }
        }

        if (node.GenericParameters.Count != 0)
        {
            var paramTypes = node.GenericParameters
                .Select(x => x.TypeInfo)
                .OfType<ITypeInfo>().ToList();

            if (paramTypes.Count != node.GenericParameters.Count) return VisitResult.SkipChildren;

            typeRef = typeRef?.MakeGenericType(paramTypes);
        }
        
        for (var i = 0; i < node.ArrayDefinitions.Count; i++)
        {
            typeRef = typeRef?.MakeArrayType();
        }
        
        node.TypeInfo = typeRef;
        
        return VisitResult.SkipChildren;
    }
    
    protected override VisitResult PostVisitLiteral(LiteralNode literalNode, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.InnerExpressionTypeStack.Push(literalNode.Type);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitMember(MemberNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var assignmentTarget = parent is AssignNode assign && assign.Targets.Contains(node);
        if (assignmentTarget)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }
        
        if (context.TryGetVariable(node.MemberName, out var variable))
        {
            context.InnerExpressionTypeStack.Push(variable.Type?.TypeInfo);
            return VisitResult.Continue;
        }

        if (context.Arguments.TryGetValue(node.MemberName, out var arg))
        {
            context.InnerExpressionTypeStack.Push(arg);
            return VisitResult.Continue;
        }
        
        var record = PlampExceptionInfo.CannotFindMember();
        SetExceptionToSymbol(node, record, context);
        context.InnerExpressionTypeStack.Push(null);
        return VisitResult.SkipChildren;
    }
    
    protected override VisitResult PostVisitReturn(ReturnNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        ITypeInfo? returnType = null;
        var functionReturnType = context.CurrentFunc?.ReturnType.TypeInfo;
        if (node.ReturnValue != null)
        {
            returnType = context.InnerExpressionTypeStack.Pop();
        }
        
        if (context.CurrentFunc?.ReturnType.TypeInfo == null) return VisitResult.SkipChildren;
        
        if (!Builtins.Void.Equals(context.CurrentFunc.ReturnType.TypeInfo) && node.ReturnValue is null)
        {
            var record = PlampExceptionInfo.ReturnValueIsMissing();
            SetExceptionToSymbol(node, record, context);
        }
        else if (Builtins.Void.Equals(context.CurrentFunc.ReturnType.TypeInfo) && node.ReturnValue is not null)
        {
            var record = PlampExceptionInfo.CannotReturnValue();
            SetExceptionToSymbol(node, record, context);
        }
        else if (functionReturnType is not null && !Builtins.Void.Equals(functionReturnType) && returnType is not null)
        {
            if (returnType.Equals(functionReturnType)) return VisitResult.SkipChildren;
            if (TryExpandType(node.ReturnValue!, returnType, functionReturnType, context)) return VisitResult.SkipChildren;
            
            var record = PlampExceptionInfo.ReturnTypeMismatch();
            SetExceptionToSymbol(node, record, context);
        }
        
        return VisitResult.SkipChildren;
    }

    #endregion

    #region Helper logic

    private bool Arithmetic(BaseBinaryNode baseBinary) =>
        baseBinary is AddNode or SubNode or MulNode or DivNode or ModuloNode;

    private bool BinaryLogicGate(BaseBinaryNode baseBinary) => baseBinary is OrNode or AndNode;

    private bool ComparisionNode(BaseBinaryNode baseBinary) => baseBinary is EqualNode or NotEqualNode or LessNode
        or LessOrEqualNode or GreaterNode or GreaterOrEqualNode;

    #endregion

    protected override TypeInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, TypeInferenceInnerContext innerContext) => new(innerContext);
}