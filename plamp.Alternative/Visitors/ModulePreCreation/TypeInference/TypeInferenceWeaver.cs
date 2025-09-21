using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
using BindingFlags = System.Reflection.BindingFlags;

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

    protected override VisitResult PreVisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        if (context.Arguments.ContainsKey(node.Name.Value))
        {
            var record = PlampExceptionInfo.ArgumentAlreadyDefined();
            SetExceptionToSymbol(node, record, context);
        }

        if (context.VariableDefinitions.TryGetValue(node.Name.Value, out var other))
        {
            var record = PlampExceptionInfo.DuplicateVariableDefinition();
            SetExceptionToSymbol(node, record, context);
            SetExceptionToSymbol(other.Variable, record, context);
            return VisitResult.SkipChildren;
        }

        var position = context.InstructionInScopePosition;
        context.AddVariableWithPosition(node, position);

        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var type = node.Type?.Symbol;
        if (type == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        context.InnerExpressionTypeStack.Push(type);
        if (parent is AssignNode) return VisitResult.Continue;
        
        var assign = WeaveAssignmentDefault(node, context);
        if(assign != null) Replace(node, assign, context);
        return VisitResult.Continue;
    }

    #endregion

    #region n-ary operators

    protected override VisitResult PostVisitUnary(BaseUnaryNode unaryNode, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var type = context.InnerExpressionTypeStack.Pop();
        if (type == typeof(bool) && unaryNode is NotNode) return VisitResult.Continue;
        
        if (type is not null
            && Numeric(type)
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
        context.InnerExpressionTypeStack.Push(unaryNode is NotNode ? typeof(bool) : null);

        return VisitResult.Continue;
    }
    
    protected override VisitResult PostVisitBinary(BaseBinaryNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        if (node is BaseAssignNode) return VisitResult.Continue;
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
        Type? leftType,
        Type? rightType,
        TypeInferenceInnerContext context)
    {
        if ((leftType != null && leftType != typeof(bool)) || (rightType != null && rightType != typeof(bool)))
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            SetExceptionToSymbol(node, record, context);
        }

        context.InnerExpressionTypeStack.Push(typeof(bool));
        return VisitResult.Continue;
    }
    
    private VisitResult ValidateBinaryComparision(
        BaseBinaryNode node,
        Type? leftType, 
        Type? rightType, 
        TypeInferenceInnerContext context)
    {
        context.InnerExpressionTypeStack.Push(typeof(bool));
        if (leftType != null
            && rightType != null
            && leftType == typeof(bool)
            && leftType == rightType
            && node is NotEqualNode or EqualNode)
        {
            return VisitResult.Continue;
        }

        if (leftType != null && rightType != null && Numeric(leftType) && Numeric(rightType))
        {
            if (leftType == rightType 
                || TryExpandTypeForBinaryNode(node.Left, node.Right, leftType, rightType, context, out _))
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
        Type? leftType, 
        Type? rightType, 
        TypeInferenceInnerContext context)
    {
        var record = PlampExceptionInfo.CannotApplyOperator();   
        if ((leftType != null && !Numeric(leftType)) || (rightType != null && !Numeric(rightType)))
        {
            context.InnerExpressionTypeStack.Push(null);
            SetExceptionToSymbol(node, record, context);
            return VisitResult.Continue;
        }

        if (leftType != null && Numeric(leftType) && rightType != null && Numeric(rightType) && leftType != rightType)
        {
            if (TryExpandTypeForBinaryNode(node.Left, node.Right, leftType, rightType, context, out var resultType))
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

    private void ExpandType(NodeBase from, Type toType, Type fromType, TypeInferenceInnerContext context)
    {
        if(fromType == toType) return;
        var toTypeNode = new TypeNode(new TypeNameNode(toType.Name));
        context.SymbolTable.AddSymbol(toTypeNode, default, default);
        toTypeNode.SetType(toType);
        var expanded = new CastNode(toTypeNode, from);
        expanded.SetFromType(fromType);
        Replace(from, expanded, context);
    }
    
    private static readonly FrozenDictionary<Type, int> TypePriorityDict = new Dictionary<Type, int>()
    {
        [typeof(double)] = 6,
        [typeof(float)] = 5,
        [typeof(ulong)] = 4,
        [typeof(long)] = 4,
        [typeof(uint)] = 3,
        [typeof(int)] = 3,
        [typeof(short)] = 2,
        [typeof(byte)] = 1,
        
    }.ToFrozenDictionary();

    private bool TryExpandNodeType(NodeBase node, Type toType, Type fromType, TypeInferenceInnerContext context)
    {
        if (fromType.IsAssignableTo(toType))
        {
            ExpandType(node, toType, fromType, context);
            return true;
        }
        
        var toExpand = ChooseChildToExpand(toType, fromType);
        if (toExpand is not (0 or -1)) return false;
        ExpandType(node, toType, fromType, context);
        return true;
    }
    
    private bool TryExpandTypeForBinaryNode(
        NodeBase left, 
        NodeBase right, 
        Type leftType, 
        Type rightType, 
        TypeInferenceInnerContext context,
        [NotNullWhen(true)] out Type? resultType)
    {
        resultType = null;
        var toExpand = ChooseChildToExpand(leftType, rightType);
        switch (toExpand)
        {
            case 1:
                ExpandType(left, rightType, leftType, context);
                resultType = rightType;
                return true;
            case -1:
                ExpandType(right, leftType, rightType, context);
                resultType = leftType;
                return true;
            case 0:
                resultType = leftType;
                return true;
            case -2: break;
            case 4:
                resultType = typeof(long);
                ExpandType(left, typeof(long), leftType, context);
                ExpandType(right, typeof(long), rightType, context);
                return true;
        }

        return false;
    }
    
    private int ChooseChildToExpand(Type leftType, Type rightType)
    {
        if (leftType == rightType) return 0;
        if (!TypePriorityDict.TryGetValue(leftType, out var leftPriority) 
            || !TypePriorityDict.TryGetValue(rightType, out var rightPriority)) return -2;
            
        if (leftPriority == rightPriority && rightPriority == 3) return 4;
        if (leftPriority == rightPriority) return -2;
        if (leftPriority > rightPriority) return -1;
        if (leftPriority < rightPriority) return 1;

        return 0;
    }

    #endregion
    
    #region Arrays
    
    protected override VisitResult PostVisitInitArray(InitArrayNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var lengthType = context.InnerExpressionTypeStack.Pop();
        if (lengthType == null)
        {
            SetExceptionToSymbol(node, PlampExceptionInfo.ArrayInitializationMustHasLength(), context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }
        
        if(!TryExpandNodeType(node.LengthDefinition, typeof(int), lengthType, context))
        {
            SetExceptionToSymbol(node.LengthDefinition, PlampExceptionInfo.ArrayLengthMustBeInteger(), context);
        }

        var arrType = default(Type);
        if (node.ArrayItemType.Symbol != null)
        {
            arrType = node.ArrayItemType.Symbol.MakeArrayType(1);
        }

        context.InnerExpressionTypeStack.Push(arrType);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitElemGetter(ElemGetterNode node, TypeInferenceInnerContext context, NodeBase? parent) 
        => PostVisitElementPropertyGeneral(node, node.ArrayIndexer.IndexMember, null, null, context, node.SetItemType);
    
    protected override VisitResult PostVisitElemSetter(ElemSetterNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var valueType = context.InnerExpressionTypeStack.Pop();
        return PostVisitElementPropertyGeneral(node, node.ArrayIndexer.IndexMember, valueType, node.Value, context, node.SetItemType);
    }

    private VisitResult PostVisitElementPropertyGeneral(
        NodeBase elementIndexerNode, 
        NodeBase indexerMember,
        Type? setValueType,
        NodeBase? setValue,
        TypeInferenceInnerContext context,
        Action<Type> innerTypeSetter)
    {
        var indexerType = context.InnerExpressionTypeStack.Pop();
        var arrayType = context.InnerExpressionTypeStack.Pop();

        if (arrayType == null)
        {
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.Continue;
        }

        if (!arrayType.IsArray)
        {
            context.InnerExpressionTypeStack.Push(null);
            var record = PlampExceptionInfo.IndexerIsNotApplicable();
            SetExceptionToSymbol(elementIndexerNode, record, context);
            return VisitResult.Continue;
        }

        var typeDimensionsCount = arrayType.GetArrayRank();
        var returnType = arrayType.GetElementType();
        context.InnerExpressionTypeStack.Push(returnType);
        if (typeDimensionsCount != 1) throw new Exception("Parser code error. Language cannot handle multidimensional array");

        var itemType = arrayType.GetElementType()!;
        innerTypeSetter(itemType);
        if (setValueType != null && setValue != null && !TryExpandNodeType(setValue, itemType, setValueType, context))
        {
            var record = PlampExceptionInfo.CannotAssign();
            SetExceptionToSymbol(elementIndexerNode, record, context);
        }
        
        if (indexerType == null)
        {
            var record = PlampExceptionInfo.EmptyIndexer();
            SetExceptionToSymbol(elementIndexerNode, record, context);
            return VisitResult.Continue;
        }
        
        if (!TryExpandNodeType(indexerMember, typeof(int), indexerType, context))
        {
            var record = PlampExceptionInfo.IndexerValueMustBeInteger();
            SetExceptionToSymbol(indexerMember, record, context);
        }
        
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
        List<Type?> defArgTypes = [];
        Type? returnType = null;
        
        var argCount = context.InnerExpressionTypeStack.Count - context.RestoreInferenceStackSize();
        if (argCount < 0) throw new InvalidOperationException("Parser error, expression type stack invalid");
        var argTypes = new List<Type?>();
        for (var i = 0; i < argCount; i++)
        {
            argTypes.Add(context.InnerExpressionTypeStack.Pop());
        }
        argTypes.Reverse();

        var intrinsic = default(MethodInfo);
        if (argTypes.All(x => x != null))
        {
            intrinsic = TypeResolveHelper.TryGetIntrinsic(node.Name.Value, argTypes.ToArray()!);
        }
        
        if (intrinsic != null)
        {
            defArgTypes = intrinsic.GetParameters().Select(p => p.ParameterType).ToList()!;
            returnType = intrinsic.ReturnType;
        }
        
        if (!context.Functions.TryGetValue(node.Name.Value, out var def) && intrinsic == null)
        {
            var exceptionRecord = PlampExceptionInfo.UnknownFunction();
            SetExceptionToSymbol(node, exceptionRecord, context);
            return VisitResult.Continue;
        }

        if (def != null)
        {
            defArgTypes = def.ParameterList.Select(x => x.Type.Symbol).ToList();
            returnType = def.ReturnType?.Symbol;
        }
        
        if (argTypes.Count != defArgTypes.Count)
        {
            var exceptionRecord = PlampExceptionInfo.UnknownFunction();
            SetExceptionToSymbol(node, exceptionRecord, context);
            context.InnerExpressionTypeStack.Push(returnType);
            return VisitResult.Continue;
        }

        var invalid = false;
        foreach (var (argType, defType, arg) in 
                 argTypes.Zip(defArgTypes).Zip(node.Args).Select(x => (x.First.First, x.First.Second, x.Second)))
        {
            if (defType == typeof(object) && defType != argType && argType != null) ExpandType(arg, defType, argType, context);
            else if (defType != null && argType != null)
            {
                if(!TryExpandNodeType(arg, defType, argType, context)) invalid = true;
            }
            else if(argType != defType) invalid = true;
        }

        if (!invalid)
        {
            if(intrinsic != null) node.SetInfo(intrinsic);
            context.InnerExpressionTypeStack.Push(returnType);
            return VisitResult.Continue;
        }
        
        var record = PlampExceptionInfo.UnknownFunction();
        SetExceptionToSymbol(node, record, context);
        return VisitResult.Continue;
    }

    #endregion

    #region Looping

    protected override VisitResult PreVisitWhile(WhileNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        VisitNodeBase(node.Condition, context, node);
        var predicateType = context.InnerExpressionTypeStack.Pop();
        if (predicateType != null && predicateType != typeof(bool))
        {
            var record = PlampExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
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
        if (predicateType != null && predicateType != typeof(bool))
        {
            var record = PlampExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }

        VisitNodeBase(node.IfClause, context, node);
        if (node.ElseClause != null) VisitNodeBase(node.ElseClause, context, node);
        return VisitResult.SkipChildren;
    }

    #endregion

    #region Assignment
    
    protected override VisitResult PostVisitAssign(AssignNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        var rightType = context.InnerExpressionTypeStack.Pop();
        var leftType = context.InnerExpressionTypeStack.Pop();
        if (rightType is not null && rightType == typeof(void))
        {
            var record = PlampExceptionInfo.CannotAssignNone();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }

        if (node.Left is VariableDefinitionNode)
        {
            ValidateAssignmentToDefinition(node, leftType, context, rightType);
            return VisitResult.SkipChildren;
        }
        
        //Then left is member or parser error
        if (node.Left is not MemberNode leftMember) throw new Exception("Parser exception, invalid ast");
        
        if (context.VariableDefinitions.TryGetValue(leftMember.MemberName, out var withPosition))
        {
            ValidateAssignmentToDefinition(node, leftType, context, rightType);
            ValidateExistingDefinition(leftMember, context, withPosition);
            return VisitResult.SkipChildren;
        }

        CreateVariableDefinitionFromMember(leftMember, context, rightType);
        return VisitResult.Continue;
    }
    
    private void ValidateAssignmentToDefinition(
        AssignNode assign, 
        Type? leftType, 
        TypeInferenceInnerContext context,
        Type? rightType)
    {
        if (leftType == null || rightType == null || leftType == rightType) return;
        if (TryExpandNodeType(assign.Right, leftType, rightType, context)) return;
        var record = PlampExceptionInfo.CannotAssign();
        SetExceptionToSymbol(assign, record, context);
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

    private void CreateVariableDefinitionFromMember(MemberNode leftMember, TypeInferenceInnerContext context, Type? rightType)
    {
        if (!context.SymbolTable.TryGetSymbol(leftMember, out var symbol))
            throw new ArgumentException("Parser error, symbol should exist");
        TypeNode? typeNode = null;
        if (rightType != null)
        {
            var typeName = new TypeNameNode(rightType.Name);
            context.SymbolTable.AddSymbol(typeName, symbol.Key, symbol.Value);
            typeNode = new TypeNode(typeName);
            typeNode.SetType(rightType);
            context.SymbolTable.AddSymbol(typeNode, symbol.Key, symbol.Value);
        }

        var variableName = new VariableNameNode(leftMember.MemberName);
        context.SymbolTable.AddSymbol(variableName, symbol.Key, symbol.Value);
        var variableNode = new VariableDefinitionNode(typeNode, variableName);
        Replace(leftMember, variableNode, context);
        context.VariableDefinitions[leftMember.MemberName] = new VariableWithPosition(variableNode, context.InstructionInScopePosition);
    }

    private AssignNode? WeaveAssignmentDefault(VariableDefinitionNode variableDefinition, TypeInferenceInnerContext context)
    {
        if (variableDefinition.Type is null) throw new Exception("Syntax analyzer exception");
        var type = TypeResolveHelper.ResolveType(
            variableDefinition.Type,
            context.Exceptions,
            context.SymbolTable,
            context.FileName);

        if (type is null) return null;
        if (type is { IsValueType: false, IsPrimitive: false })
        {
            return new AssignNode(variableDefinition, new LiteralNode(null, type));
        }
        
        if (type is { IsPrimitive: true })
        {
            object? value = null;
            if (type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(float)
                || type == typeof(char)
                || type == typeof(double)) value = 0;
            if (type == typeof(bool)) value = false;
            //for simple types such as integer or float
            return new AssignNode(variableDefinition, new LiteralNode(value, type));
        }

        //for structures
        var ctor = type.GetConstructor(BindingFlags.Public, [])!;
        var ctorNode = new ConstructorCallNode(variableDefinition.Type, []);
        ctorNode.SetConstructorInfo(ctor);
        var assign = new AssignNode(variableDefinition, ctorNode);
        return assign;
    }
    
    #endregion
    
    #region Misc
    
    protected override VisitResult PreVisitType(TypeNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        if(node.Symbol != null) return VisitResult.Continue;
        var type = TypeResolveHelper.ResolveType(node, context.Exceptions, context.SymbolTable, context.FileName);
        if(type != null) node.SetType(type);
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
        var assignmentSource = parent is not AssignNode assign || assign.Left != node;
        if (!context.VariableDefinitions.TryGetValue(node.MemberName, out var withPosition) 
            && !context.Arguments.TryGetValue(node.MemberName, out arg)
            && assignmentSource)
        {
            var record = PlampExceptionInfo.CannotFindMember();
            SetExceptionToSymbol(node, record, context);
            context.InnerExpressionTypeStack.Push(null);
            return VisitResult.SkipChildren;
        }
        
        var memberType = withPosition?.Variable.Type?.Symbol ?? arg?.Type.Symbol;
        context.InnerExpressionTypeStack.Push(memberType);
        return VisitResult.Continue;
    }
    
    protected override VisitResult PostVisitReturn(ReturnNode node, TypeInferenceInnerContext context, NodeBase? parent)
    {
        Type? returnType = null;
        var functionReturnType = context.CurrentFunc?.ReturnType?.Symbol;
        if (node.ReturnValue != null)
        {
            returnType = context.InnerExpressionTypeStack.Pop();
        }
        
        if (context.CurrentFunc?.ReturnType?.Symbol == null) return VisitResult.SkipChildren;
        
        if (context.CurrentFunc?.ReturnType?.Symbol != typeof(void) && node.ReturnValue is null)
        {
            var record = PlampExceptionInfo.ReturnValueIsMissing();
            SetExceptionToSymbol(node, record, context);
        }
        else if (context.CurrentFunc?.ReturnType?.Symbol == typeof(void) && node.ReturnValue is not null)
        {
            var record = PlampExceptionInfo.CannotReturnValue();
            SetExceptionToSymbol(node, record, context);
        }
        else if (functionReturnType is not null && functionReturnType != typeof(void) && returnType is not null)
        {
            if (returnType == functionReturnType) return VisitResult.SkipChildren;
            if (TryExpandNodeType(node.ReturnValue!, functionReturnType, returnType, context)) return VisitResult.SkipChildren;
            
            var record = PlampExceptionInfo.ReturnTypeMismatch();
            SetExceptionToSymbol(node, record, context);
        }
        
        return VisitResult.SkipChildren;
    }

    #endregion

    #region Helper logic

    private bool Numeric(Type type) => type == typeof(int) || type == typeof(uint) || type == typeof(long) ||
                                       type == typeof(ulong) || type == typeof(byte) || type == typeof(float) ||
                                       type == typeof(double);

    private bool Arithmetic(BaseBinaryNode baseBinary) =>
        baseBinary is AddNode or SubNode or MulNode or DivNode or ModuloNode;

    private bool BinaryLogicGate(BaseBinaryNode baseBinary) => baseBinary is OrNode or AndNode;

    private bool ComparisionNode(BaseBinaryNode baseBinary) => baseBinary is EqualNode or NotEqualNode or LessNode
        or LessOrEqualNode or GreaterNode or GreaterOrEqualNode;

    #endregion

    protected override TypeInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(TypeInferenceInnerContext innerContext, PreCreationContext outerContext) => new(innerContext);
}