using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Modification;
using BindingFlags = System.Reflection.BindingFlags;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public class TypeInferenceWeaver : BaseWeaver<PreCreationContext, TypeInferenceInnerContext>
{
    protected override VisitResult VisitDef(DefNode node, TypeInferenceInnerContext context)
    {
        context.CurrentFunc = node;
        foreach (var parameterNode in node.ParameterList)
        {
            context.Arguments.Add(parameterNode.Name.MemberName, parameterNode);
        }

        VisitInternal(node.Body, context);
        
        context.Arguments.Clear();
        context.CurrentFunc = null;
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitBody(BodyNode node, TypeInferenceInnerContext context)
    {
        context.EnterBody();
        
        foreach (var child in node.Visit())
        {
            VisitNodeBase(child, context);
            //TODO: to separate method
            if (child is VariableDefinitionNode emptyDef)
            {
                var assign = WeaveAssignmentDefault(emptyDef, context);
                if(assign != null) Replace(child, assign, context);
            }
            context.NextInstruction();
        }

        context.ExitBody();
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitVariableDefinition(VariableDefinitionNode node, TypeInferenceInnerContext context)
    {
        if (context.Arguments.ContainsKey(node.Member.MemberName))
        {
            var record = PlampExceptionInfo.ArgumentAlreadyDefined();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            return VisitResult.SkipChildren;
        }

        if (context.VariableDefinitions.TryGetValue(node.Member.MemberName, out var other))
        {
            var record = PlampExceptionInfo.DuplicateVariableDefinition();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(other.Variable, record, context.FileName));
            return VisitResult.SkipChildren;
        }

        var position = context.InstructionInScopePosition;
        context.VariableDefinitions[node.Member.MemberName] = new VariableWithPosition(node, position);

        if (node.Type == null) return VisitResult.SkipChildren;
        var variableType = TypeResolveHelper.ResolveType(node.Type, context.Exceptions, context.SymbolTable, context.FileName);
        if (variableType == null) return VisitResult.SkipChildren;
        
        node.Type.SetType(variableType);
        context.InnerExpressionType = variableType;
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, TypeInferenceInnerContext context)
    {
        VisitNodeBase(unaryNode.Inner, context);
        if (context.InnerExpressionType == typeof(bool)
            && unaryNode is not NotNode)
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(unaryNode, record, context.FileName));
            context.InnerExpressionType = null;
        }
        else if (context.InnerExpressionType != typeof(bool) && unaryNode is NotNode)
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(unaryNode, record, context.FileName));
            context.InnerExpressionType = typeof(bool);
        }
        else if (context.InnerExpressionType != null && !Numeric(context.InnerExpressionType) &&
                unaryNode is PrefixIncrementNode or PrefixDecrementNode or PostfixDecrementNode or PostfixIncrementNode or UnaryMinusNode)
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(unaryNode, record, context.FileName));
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
        if (node is BaseAssignNode) return base.VisitBinaryExpression(node, context);
        
        VisitNodeBase(node.Left, context);
        var leftType = context.InnerExpressionType;
        VisitNodeBase(node.Right, context);
        var rightType = context.InnerExpressionType;
        context.InnerExpressionType = null;

        if (leftType == null && rightType == null) return VisitResult.SkipChildren;

        if (Arithmetic(node)) return ValidateBinaryArithmetic(node, leftType, rightType, context);
        if (ComparisionNode(node)) return ValidateBinaryComparision(node, leftType, rightType, context);
        if (BinaryLogicGate(node)) return ValidateBinaryLogical(node, leftType, rightType, context);
        throw new ArgumentException("Unexpected binary operator type");
    }

    protected override VisitResult VisitCall(CallNode node, TypeInferenceInnerContext context)
    {
        List<Type?> defArgTypes = [];
        Type? returnType = null;
        var intrinsic = TypeResolveHelper.TryGetIntrinsic(node.MethodName.MemberName);
        if (intrinsic != null)
        {
            defArgTypes = intrinsic.GetParameters().Select(p => p.ParameterType).ToList()!;
            returnType = intrinsic.ReturnType;
        }
        
        if (!context.Functions.TryGetValue(node.MethodName.MemberName, out var def) && intrinsic == null)
        {
            AddUnexpectedCallExceptionAndValidateChildren();
            context.InnerExpressionType = null;
            return VisitResult.SkipChildren;
        }

        if (def != null)
        {
            defArgTypes = def.ParameterList.Select(x => x.Type.Symbol).ToList();
            returnType = def.ReturnType?.Symbol;
        }

        context.InnerExpressionType = returnType;

        if (node.Args.Count != defArgTypes.Count)
        {
            AddUnexpectedCallExceptionAndValidateChildren();
            return VisitResult.SkipChildren;
        }

        var invalid = false;
        foreach (var (arg, defType) in node.Args.Zip(defArgTypes))
        {
            VisitInternal(arg, context);
            var argType = context.InnerExpressionType;
            if (argType != defType) invalid = true;
        }

        if (!invalid) return VisitResult.SkipChildren;
        
        var record = PlampExceptionInfo.UnknownFunction();
        context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        return VisitResult.SkipChildren;
        
        void AddUnexpectedCallExceptionAndValidateChildren()
        {
            var exceptionRecord = PlampExceptionInfo.UnknownFunction();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, exceptionRecord, context.FileName));
            foreach (var parameter in node.Args)
            {
                VisitChildren(parameter, context);
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
        ParameterNode? arg = null;
        if (!context.VariableDefinitions.TryGetValue(node.MemberName, out var withPosition) 
            && !context.Arguments.TryGetValue(node.MemberName, out arg))
        {
            var record = PlampExceptionInfo.CannotFindMember();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            context.InnerExpressionType = null;
            return VisitResult.SkipChildren;
        }
        
        context.InnerExpressionType = withPosition?.Variable.Type?.Symbol ?? arg?.Type.Symbol;
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitAssign(AssignNode node, TypeInferenceInnerContext context)
    {
        VisitNodeBase(node.Right, context);
        var rightType = context.InnerExpressionType;
        if (rightType is not null && rightType == typeof(void))
        {
            var record = PlampExceptionInfo.CannotAssignNone();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }
        
        context.InnerExpressionType = null;
        if (node.Left is VariableDefinitionNode leftDef) return ValidateAssignmentToDefinition(node, leftDef, context, rightType);
        
        //Then left is member or parser error
        if (node.Left is not MemberNode leftMember) throw new Exception("Parser exception, invalid ast");
        
        if (context.VariableDefinitions.TryGetValue(leftMember.MemberName, out var withPosition))
        {
            return ValidateExistingDefinition(node, leftMember, context, withPosition, rightType);
        }
        
        TypeNode? typeNode = null;
        if (rightType != null) typeNode = CreateTypeForMember(leftMember, context, rightType);

        var definition = new VariableDefinitionNode(typeNode, leftMember);
        Replace(leftMember, definition, context);
        
        context.VariableDefinitions[leftMember.MemberName] = new VariableWithPosition(definition, context.InstructionInScopePosition);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitWhile(WhileNode node, TypeInferenceInnerContext context)
    {
        VisitNodeBase(node.Condition, context);
        var predicateType = context.InnerExpressionType;
        if (predicateType != null && predicateType != typeof(bool))
        {
            var record = PlampExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }

        VisitNodeBase(node.Body, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitCondition(ConditionNode node, TypeInferenceInnerContext context)
    {
        VisitNodeBase(node.Predicate, context);
        var predicateType = context.InnerExpressionType;
        if (predicateType != null && predicateType != typeof(bool))
        {
            var record = PlampExceptionInfo.PredicateMustBeBooleanType();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }

        VisitNodeBase(node.IfClause, context);
        if (node.ElseClause != null) VisitNodeBase(node.ElseClause, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitReturn(ReturnNode node, TypeInferenceInnerContext context)
    {
        Type? returnType = null;
        if (node.ReturnValue is not null)
        {
            VisitInternal(node.ReturnValue, context);
            returnType = context.InnerExpressionType;
        }

        if (context.CurrentFunc?.ReturnType?.Symbol == null) return VisitResult.SkipChildren;
        
        if (context.CurrentFunc?.ReturnType?.Symbol != typeof(void) && node.ReturnValue is null)
        {
            var record = PlampExceptionInfo.ReturnValueIsMissing();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }
        else if (context.CurrentFunc?.ReturnType?.Symbol == typeof(void) && node.ReturnValue is not null)
        {
            var record = PlampExceptionInfo.CannotReturnValue();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }
        else if (context.CurrentFunc?.ReturnType?.Symbol != typeof(void) && returnType is not null)
        {
            if (returnType == context.CurrentFunc?.ReturnType?.Symbol) return VisitResult.SkipChildren;
            
            var record = PlampExceptionInfo.ReturnTypeMismatch();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }
        
        return VisitResult.SkipChildren;
    }

    private VisitResult ValidateBinaryLogical(
        BaseBinaryNode node,
        Type? leftType,
        Type? rightType,
        TypeInferenceInnerContext context)
    {
        if ((leftType != null && leftType != typeof(bool)) || (rightType != null && rightType != typeof(bool)))
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }

        context.InnerExpressionType = typeof(bool);
        return VisitResult.SkipChildren;
    }
    
    private VisitResult ValidateBinaryComparision(
        BaseBinaryNode node,
        Type? leftType, 
        Type? rightType, 
        TypeInferenceInnerContext context)
    {
        context.InnerExpressionType = typeof(bool);
        if (leftType != null && rightType != null && leftType != rightType)
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            return VisitResult.SkipChildren;
        }
            
        if((leftType != null && !Numeric(leftType)) 
           || (rightType != null && !Numeric(rightType) && node is not EqualNode and not NotEqualNode))
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
        }

        return VisitResult.SkipChildren;
    }
    
    private VisitResult ValidateBinaryArithmetic(
        BaseBinaryNode node,
        Type? leftType, 
        Type? rightType, 
        TypeInferenceInnerContext context)
    {
        if ((leftType != null && !Numeric(leftType)) || (rightType != null && !Numeric(rightType))
                                                     || leftType != rightType)
        {
            var record = PlampExceptionInfo.CannotApplyOperator();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            context.InnerExpressionType = null;
            return VisitResult.SkipChildren;
        }

        context.InnerExpressionType = leftType ?? rightType;
        return VisitResult.SkipChildren;
    }

    private VisitResult ValidateAssignmentToDefinition(
        AssignNode assign, 
        VariableDefinitionNode left, 
        TypeInferenceInnerContext context,
        Type? rightType)
    {
        VisitVariableDefinition(left, context);
        var leftType = context.InnerExpressionType;
        if (leftType == null || rightType == null || leftType == rightType) return VisitResult.SkipChildren;
        var record = PlampExceptionInfo.CannotAssign();
        context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(assign, record, context.FileName));
        return VisitResult.SkipChildren;
    }

    private VisitResult ValidateExistingDefinition(
        AssignNode assign,
        MemberNode left,
        TypeInferenceInnerContext context,
        VariableWithPosition existingVar,
        Type? rightType)
    {
        PlampExceptionRecord record;
        if (context.InstructionInScopePosition.Depth < existingVar.InScopePositionList.Depth)
        {
            record = PlampExceptionInfo.DuplicateVariableDefinition();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(existingVar.Variable, record, context.FileName));
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(left, record, context.FileName));
            return VisitResult.SkipChildren;
        }

        if (existingVar.Variable.Type?.Symbol is not {} leftType
            || rightType == null 
            || leftType == rightType)
        {
            return VisitResult.SkipChildren;
        }
            
        record = PlampExceptionInfo.CannotAssign();
        context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(assign, record, context.FileName));
        return VisitResult.SkipChildren;
    }

    private TypeNode CreateTypeForMember(MemberNode leftMember, TypeInferenceInnerContext context, Type rightType)
    {
        if (!context.SymbolTable.TryGetSymbol(leftMember, out var symbol))
            throw new ArgumentException("Parser error, symbol should exist");
                
        var typeName = new MemberNode(rightType.Name);
        context.SymbolTable.AddSymbol(typeName, symbol.Key, symbol.Value);
        var typeNode = new TypeNode(typeName, []);
        typeNode.SetType(rightType);
        context.SymbolTable.AddSymbol(typeNode, symbol.Key, symbol.Value);
        return typeNode;
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
    
    private bool Numeric(Type type) => type == typeof(int) || type == typeof(uint) || type == typeof(long) ||
                                         type == typeof(ulong) || type == typeof(byte) || type == typeof(float) ||
                                         type == typeof(double);

    private bool Arithmetic(BaseBinaryNode baseBinary) =>
        baseBinary is PlusNode or MinusNode or MultiplyNode or DivideNode or ModuloNode;

    private bool BinaryLogicGate(BaseBinaryNode baseBinary) => baseBinary is OrNode or AndNode;

    private bool ComparisionNode(BaseBinaryNode baseBinary) => baseBinary is EqualNode or NotEqualNode or LessNode
        or LessOrEqualNode or GreaterNode or GreaterOrEqualNode;

    protected override TypeInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(TypeInferenceInnerContext innerContext, PreCreationContext outerContext) => new(innerContext);
}