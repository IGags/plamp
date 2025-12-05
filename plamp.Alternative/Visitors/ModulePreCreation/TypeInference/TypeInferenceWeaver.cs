using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions;
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
using plamp.Intrinsics;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public class TypeInferenceWeaver : BaseWeaver<PreCreationContext, TypeInferenceInnerContext>
{
    #region TopLevel

    protected override VisitResult PreVisitFunction(FuncNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.CurrentFunc = node;
        var nonDupArgs = node.ParameterList
            .GroupBy(x => x.Name)
            .Where(x => x.Count() == 1)
            .SelectMany(x => x);
        
        foreach (var arg in nonDupArgs) context.Arguments.Add(arg.Name.Value, arg);
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
        
        if (context.VariableDefinitions.TryGetValue(node.Value, out var other))
        {
            var record = PlampExceptionInfo.DuplicateVariableDefinition();
            SetExceptionToSymbol(node, record, context);
            SetExceptionToSymbol(other.Variable, record, context);
            exception = true;
        }

        if (!exception && parent is VariableDefinitionNode def)
        {
            var position = context.InstructionInScopePosition;
            context.AddVariableWithPosition(node, def, position);
        }

        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var type = node.Type?.TypedefRef;
        if (type == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        context.InnerExpressionTypeStack.Push(type);
        if (parent is AssignNode) return VisitResult.Continue;
        
        var assign = WeaveAssignmentDefault(node);
        if(assign != null) Replace(node, assign, context);
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

        if (RuntimeSymbols.GetSymbolTable.IsLogical(type) && unaryNode is NotNode)
        {
            context.InnerExpressionTypeStack.Push(RuntimeSymbols.GetSymbolTable.MakeLogical());
            return VisitResult.Continue;
        }
        
        if (RuntimeSymbols.GetSymbolTable.IsNumeric(type)
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
        ICompileTimeType? leftType,
        ICompileTimeType? rightType,
        TypeInferenceInnerContext context)
    {
        if (   (leftType  != null && !RuntimeSymbols.GetSymbolTable.IsLogical(leftType)) 
            || (rightType != null && !RuntimeSymbols.GetSymbolTable.IsLogical(rightType)))
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            SetExceptionToSymbol(node, record, context);
        }

        context.InnerExpressionTypeStack.Push(RuntimeSymbols.GetSymbolTable.MakeLogical());
        return VisitResult.Continue;
    }
    
    private VisitResult ValidateBinaryComparision(
        BaseBinaryNode node,
        ICompileTimeType? leftType, 
        ICompileTimeType? rightType, 
        TypeInferenceInnerContext context)
    {
        context.InnerExpressionTypeStack.Push(RuntimeSymbols.GetSymbolTable.MakeLogical());
        if (leftType != null
            && rightType != null
            && RuntimeSymbols.GetSymbolTable.IsLogical(leftType)
            && RuntimeSymbols.GetSymbolTable.IsLogical(rightType)
            && node is NotEqualNode or EqualNode)
        {
            return VisitResult.Continue;
        }

        if (   leftType  != null 
            && rightType != null 
            && RuntimeSymbols.GetSymbolTable.IsNumeric(leftType) 
            && RuntimeSymbols.GetSymbolTable.IsNumeric(rightType))
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
        ICompileTimeType? leftType, 
        ICompileTimeType? rightType, 
        TypeInferenceInnerContext context)
    {
        var record = PlampExceptionInfo.CannotApplyOperator();   
        if (   (leftType  != null && !RuntimeSymbols.GetSymbolTable.IsNumeric(leftType)) 
            || (rightType != null && !RuntimeSymbols.GetSymbolTable.IsNumeric(rightType)))
        {
            context.InnerExpressionTypeStack.Push(null);
            SetExceptionToSymbol(node, record, context);
            return VisitResult.Continue;
        }

        if (leftType != null 
            && RuntimeSymbols.GetSymbolTable.IsNumeric(leftType) 
            && rightType != null 
            && RuntimeSymbols.GetSymbolTable.IsNumeric(rightType) 
            && !leftType.Equals(rightType))
        {
            if (TryExpandNumericBinary(node.Left, node.Right, leftType, rightType, context, out var resultType))
            {
                context.InnerExpressionTypeStack.Push(resultType);
                return VisitResult.Continue;
            }

            context.InnerExpressionTypeStack.Push(null);
            SetExceptionToSymbol(node, record, context);
            return VisitResult.Continue;
        }
        
        var innerType = leftType == null && rightType == null ? null : leftType ?? rightType!;
        context.InnerExpressionTypeStack.Push(innerType);
        return VisitResult.Continue;
    }
    
    #endregion

    #region TypeExpansion

    private bool TryExpandType(
        NodeBase from, 
        ICompileTimeType fromType,
        ICompileTimeType toType, 
        TypeInferenceInnerContext context)
    {
        if(fromType.Equals(toType)) return true;
        if (!RuntimeSymbols.GetSymbolTable.TypeIsImplicitlyConvertable(fromType, toType)) return false;
        var toTypeNode = new TypeNode(new TypeNameNode(toType.TypeName));
        context.TranslationTable.AddSymbol(toTypeNode, default);
        toTypeNode.SetTypeRef(toType);
        var expanded = new CastNode(toTypeNode, from);
        expanded.SetFromType(fromType);
        Replace(from, expanded, context);
        return true;
    }

    private bool TryExpandNumericBinary(
        NodeBase left,
        NodeBase right,
        ICompileTimeType leftType,
        ICompileTimeType rightType,
        TypeInferenceInnerContext context,
        [NotNullWhen(true)] out ICompileTimeType? resultType)
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

        var intType = RuntimeSymbols.GetSymbolTable.MakeInt();
        if (TryExpandBoth(left, right, leftType, rightType, intType, context))
        {
            resultType = intType;
            return true;
        }

        var longType = RuntimeSymbols.GetSymbolTable.MakeLong();
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
        ICompileTimeType leftType,
        ICompileTimeType rightType,
        ICompileTimeType target,
        TypeInferenceInnerContext context)
    {
        if (RuntimeSymbols.GetSymbolTable.TypeIsImplicitlyConvertable(leftType, target)
            && RuntimeSymbols.GetSymbolTable.TypeIsImplicitlyConvertable(rightType, target))
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

        if (!TryExpandType(node.LengthDefinition, lengthType, RuntimeSymbols.GetSymbolTable.MakeInt(), context))
        {
            SetExceptionToSymbol(node.LengthDefinition, PlampExceptionInfo.ArrayLengthMustBeInteger(), context);
        }

        ICompileTimeType? arrType = null;
        if (node.ArrayItemType.TypedefRef != null)
        {
            arrType = node.ArrayItemType.TypedefRef.MakeArrayType();
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

        if (!TryExpandType(node.IndexMember, indexerExpression, RuntimeSymbols.GetSymbolTable.MakeInt(), context))
        {
            SetExceptionToSymbol(node.IndexMember, PlampExceptionInfo.IndexerValueMustBeInteger(), context);
        }

        if (indexationTargetType.GetDefinitionInfo().ArrayUnderlyingType == null)
        {
            SetExceptionToSymbol(node.From, PlampExceptionInfo.IndexerIsNotApplicable(), context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        var elementType = indexationTargetType.GetDefinitionInfo().ArrayUnderlyingType;
        node.SetItemType(elementType);
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
        var argTypes = new List<ICompileTimeType?>();
        for (var i = 0; i < argCount; i++)
        {
            argTypes.Add(context.InnerExpressionTypeStack.Pop());
        }
        argTypes.Reverse();

        if(argTypes.All(x => x != null))
        {
            var errRecord = TypeResolveHelper.FindFuncBySignature(node.Name.Value, argTypes!, context.GetAllSymbols(), out var fnRef);
            if (errRecord != null)
            {
                SetExceptionToSymbol(node, errRecord, context);
                context.InnerExpressionTypeStack.Push(null);
                return VisitResult.SkipChildren;
            }

            for (var i = 0; i < argTypes.Count; i++)
            {
                //Так как сигнатура была определена, то правильный каст типов гарантирован.
                TryExpandType(node.Args[i], argTypes[i]!, fnRef!.ArgumentTypes[i], context);
            }
            context.InnerExpressionTypeStack.Push(fnRef!.GetDefinitionInfo().ReturnType);
            return VisitResult.Continue;
        }

        context.InnerExpressionTypeStack.Push(null);
        return VisitResult.SkipChildren;
    }

    #endregion

    #region Looping

    protected override VisitResult PreVisitWhile(WhileNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        VisitNodeBase(node.Condition, context, node);
        var predicateType = context.InnerExpressionTypeStack.Pop();
        if (predicateType != null && !RuntimeSymbols.GetSymbolTable.IsLogical(predicateType))
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
        if (predicateType != null && !RuntimeSymbols.GetSymbolTable.IsLogical(predicateType))
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

    private record AssignmentPair(ICompileTimeType? SourceType, ICompileTimeType? TargetType, NodeBase SourceNode, NodeBase TargetNode);
    
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

        var types = new List<ICompileTimeType?>(assignTypeCount);
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
            if (assignment.SourceType is not null && RuntimeSymbols.GetSymbolTable.IsVoid(assignment.SourceType))
            {
                var record = PlampExceptionInfo.CannotAssignNone();
                context.Exceptions.Add(context.TranslationTable.SetExceptionToNode(node, record));
            }

            switch (assignment.TargetNode)
            {
                case VariableDefinitionNode:
                case IndexerNode:
                    ValidateAssignmentToDefinition(node, assignment.SourceNode, assignment.TargetType, context, assignment.SourceType);
                    continue;
                case MemberNode leftMember:
                    if (!context.VariableDefinitions.TryGetValue(leftMember.MemberName, out var withPosition))
                    {
                        CreateVariableDefinitionFromMember(leftMember, context, assignment.SourceType);
                        continue;
                    }
                    
                    ValidateAssignmentToDefinition(node, assignment.SourceNode, assignment.TargetType, context, assignment.SourceType);
                    ValidateExistingDefinition(leftMember, context, withPosition);
                    continue;
                default: throw new Exception("Parser exception, invalid ast");
            }
        }
        
        return VisitResult.Continue;
    }
    
    private void ValidateAssignmentToDefinition(
        NodeBase assignNode,
        NodeBase assignmentSource, 
        ICompileTimeType? leftType, 
        TypeInferenceInnerContext context,
        ICompileTimeType? rightType)
    {
        if (leftType == null || rightType == null || leftType.Equals(rightType)) return;
        if (TryExpandType(assignmentSource, rightType, leftType, context)) return;
        var record = PlampExceptionInfo.CannotAssign();
        SetExceptionToSymbol(assignNode, record, context);
    }

    private void ValidateExistingDefinition(MemberNode left,
        TypeInferenceInnerContext context,
        VariableWithPosition existingVar)
    {
        if (context.InstructionInScopePosition.Depth >= existingVar.InScopePositionList.Depth) return;
        
        var record = PlampExceptionInfo.DuplicateVariableDefinition();
        SetExceptionToSymbol(existingVar.Variable, record, context);
        SetExceptionToSymbol(left, record, context);
    }

    private void CreateVariableDefinitionFromMember(MemberNode leftMember, TypeInferenceInnerContext context, ICompileTimeType? rightType)
    {
        if (!context.TranslationTable.TryGetSymbol(leftMember, out var symbol))
            throw new ArgumentException("Parser error, symbol should exist");
        TypeNode? typeNode = null;
        if (rightType != null)
        {
            var typeName = new TypeNameNode(rightType.TypeName);
            context.TranslationTable.AddSymbol(typeName, symbol);
            typeNode = new TypeNode(typeName);
            typeNode.SetTypeRef(rightType);
            context.TranslationTable.AddSymbol(typeNode, symbol);
        }

        var variableName = new VariableNameNode(leftMember.MemberName);
        context.TranslationTable.AddSymbol(variableName, symbol);
        var variableNode = new VariableDefinitionNode(typeNode, variableName);
        Replace(leftMember, variableNode, context);
        context.VariableDefinitions[leftMember.MemberName] = new VariableWithPosition(variableNode, context.InstructionInScopePosition);
    }

    //TODO: Может упасть с ошибкой таблицы символов. Нужно добавлять символы.
    private AssignNode? WeaveAssignmentDefault(VariableDefinitionNode variableDefinition)
    {
        if (variableDefinition.Type is null) throw new Exception("Syntax analyzer exception");
        var type = variableDefinition.Type.TypedefRef;

        if (type is null) return null;

        var arrayUnderlyingType = type.GetDefinitionInfo().ArrayUnderlyingType;
        if (arrayUnderlyingType != null)
        {
            var itemType = new TypeNode(new TypeNameNode(type.TypeName));
            itemType.SetTypeRef(arrayUnderlyingType);
            var initArrayNode = new InitArrayNode(itemType, new LiteralNode(0, RuntimeSymbols.GetSymbolTable.MakeInt()));
            // []int a; => []int a := Array.Empty<int>()
            return new AssignNode([variableDefinition], [initArrayNode]);
        }

        if (RuntimeSymbols.GetSymbolTable.IsNumeric(type))
        {
            return new AssignNode([variableDefinition], [new LiteralNode(0, type)]);
        }

        if (RuntimeSymbols.GetSymbolTable.IsLogical(type))
        {
            return new AssignNode([variableDefinition], [new LiteralNode(false, type)]);
        }

        if (RuntimeSymbols.GetSymbolTable.MakeChar().Equals(type))
        {
            return new AssignNode([variableDefinition], [new LiteralNode((char)0, type)]);
        }

        if (RuntimeSymbols.GetSymbolTable.MakeString().Equals(type))
        {
            return new AssignNode([variableDefinition], [new LiteralNode(string.Empty, type)]);
        }

        var typeNode = new TypeNode(new TypeNameNode(type.TypeName));
        typeNode.SetTypeRef(type);
        return new AssignNode([variableDefinition], [new InitTypeNode(typeNode, [])]);
    }
    
    #endregion
    
    #region Misc
    
    protected override VisitResult PreVisitType(TypeNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        if(node.TypedefRef != null) return VisitResult.Continue;
        var symbolTables = context.GetAllSymbols();
        var record = TypeResolveHelper.FindTypeByName(node.TypeName.Name, node.ArrayDefinitions, symbolTables, out var typeRef);
        if (record != null) SetExceptionToSymbol(node, record, context);
        else node.SetTypeRef(typeRef!);
        return VisitResult.SkipChildren;
    }
    
    protected override VisitResult PostVisitLiteral(LiteralNode literalNode, TypeInferenceInnerContext context, NodeBase? parent)
    {
        context.InnerExpressionTypeStack.Push(literalNode.Type);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitMember(MemberNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        ParameterNode? arg = null;
        var assignmentSource = parent is not AssignNode assign || !assign.Targets.Contains(node);
        if (!context.VariableDefinitions.TryGetValue(node.MemberName, out var withPosition) 
            && !context.Arguments.TryGetValue(node.MemberName, out arg)
            && assignmentSource)
        {
            var record = PlampExceptionInfo.CannotFindMember();
            SetExceptionToSymbol(node, record, context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }
        
        var memberType = withPosition?.Variable.Type?.TypedefRef ?? arg?.Type.TypedefRef;
        context.InnerExpressionTypeStack.Push(memberType);
        return VisitResult.Continue;
    }
    
    protected override VisitResult PostVisitReturn(ReturnNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        ICompileTimeType? returnType = null;
        var functionReturnType = context.CurrentFunc?.ReturnType?.TypedefRef;
        if (node.ReturnValue != null)
        {
            returnType = context.InnerExpressionTypeStack.Pop();
        }
        
        if (context.CurrentFunc?.ReturnType?.TypedefRef == null) return VisitResult.SkipChildren;
        
        if (!RuntimeSymbols.GetSymbolTable.IsVoid(context.CurrentFunc.ReturnType.TypedefRef) && node.ReturnValue is null)
        {
            var record = PlampExceptionInfo.ReturnValueIsMissing();
            SetExceptionToSymbol(node, record, context);
        }
        else if (RuntimeSymbols.GetSymbolTable.IsVoid(context.CurrentFunc.ReturnType.TypedefRef) && node.ReturnValue is not null)
        {
            var record = PlampExceptionInfo.CannotReturnValue();
            SetExceptionToSymbol(node, record, context);
        }
        else if (functionReturnType is not null && !RuntimeSymbols.GetSymbolTable.IsVoid(functionReturnType) && returnType is not null)
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

    protected override PreCreationContext MapInnerToOuter(TypeInferenceInnerContext innerContext, PreCreationContext outerContext) => new(innerContext);
}