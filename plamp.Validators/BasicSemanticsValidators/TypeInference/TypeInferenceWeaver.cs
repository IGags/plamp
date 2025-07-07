using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Validators.BasicSemanticsValidators.TypeInference;

/*public class TypeInferenceWeaver : BaseWeaver<TypeInferenceContext, TypeInferenceInnerContext>
{
    protected override TypeInferenceInnerContext CreateInnerContext(TypeInferenceContext context)
    {
        return new TypeInferenceInnerContext()
        {
            AssemblyContainer = context.AssemblyContainer,
            SymbolTable = context.SymbolTable,
            ModuleName = context.ModuleName,
            ImportedModules = context.ImportedModules,
            ThisModuleAssemblyContainer = context.ThisModuleAssemblyContainer
        };
    }

    protected override WeaveResult CreateWeaveResult(TypeInferenceInnerContext innerContext, TypeInferenceContext outerContext)
    {
        var result = base.CreateWeaveResult(innerContext, outerContext);
        return result with { Exceptions = innerContext.Exceptions };
    }

    protected override VisitResult VisitDef(DefNode node, TypeInferenceInnerContext context)
    {
        if (node.ReturnType is not TypeNode typeNode)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.DefNodeReturnTypeMustBeTypeNode(), node.ReturnType, null, null));
        }
        else
        {
            VisitInternal(typeNode, context);
        }
        
        VisitChildren(node.ParameterList, context);
        VisitInternal(node.Body, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitParameter(ParameterNode node, TypeInferenceInnerContext context)
    {
        var invalid = false;
        MemberNode? parameterName = null;
        if (node.Name is not MemberNode {MemberName: not null} member)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.ParameterMustHaveName(), node, null, null));
            invalid = true;
        }
        else
        {
            parameterName = member;
        }

        if (node.Type is not TypeNode typeNode)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.ParameterMustHaveType(), node, null, null));
        }
        else if (!invalid && parameterName is not null)
        {
            VisitInternal(typeNode, context);
            context.VariableTypeDict[parameterName.MemberName] = new (parameterName, context.PrevInferredType);
        }
        
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitType(TypeNode node, TypeInferenceInnerContext context)
    {
        if (!TryGetTypeAndValidateUniqueness(node, context, out var inferredType))
        {
            return VisitResult.Continue;
        }
        
        var genericTypes = new List<Type?>();
        foreach (var type in node.InnerGenerics ?? [])
        {
            if (type is not TypeNode typeNode)
            {
                context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                    PlampSemanticsExceptions.GenericMustBeType(), type, null, null));
            }
            else
            {
                VisitType(typeNode, context);
                genericTypes.Add(context.PrevInferredType);
            }
        }

        if (genericTypes is { Count: > 0 } && genericTypes.All(x => x is not null && !x.IsGenericTypeDefinition))
        {
            var type = inferredType!.Type.MakeGenericType(genericTypes.ToArray()!);
            context.PrevInferredType = type;
            
        }
        else
        {
            context.PrevInferredType = inferredType!.Type;
        }

        var newTypeNode = new TypeNode(node.TypeName, node.InnerGenerics?.ToList())
            { Symbol = context.PrevInferredType };
        
        Replace(node, newTypeNode);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitBody(BodyNode node, TypeInferenceInnerContext context)
    {
        foreach (var statement in node.ExpressionList ?? [])
        {
            if(statement == null) continue;
            VisitInternal(statement, context);
            context.PrevInferredType = null;
        }
        
        return VisitResult.SkipChildren;
    }

    //TODO: move all validation to separate methods
    protected override VisitResult VisitCall(CallNode node, TypeInferenceInnerContext context)
    {
        if (node.MethodName is not MemberNode {MemberName: not null} methodName)
        {
            context.Exceptions.Add(
                context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.MethodMustHaveName(), node, null, null));
            context.PrevInferredType = null;
            return VisitResult.SkipChildren;
        }

        Type? fromType = null;
        if (node.From != null)
        {
            VisitInternal(node.From, context);
            fromType = context.PrevInferredType;
        }

        var argTypes = GetArgTypes(node.Args, context);

        if (fromType == null)
        {
            context.PrevInferredType = null;
            return VisitResult.SkipChildren;
        }

        var type = GetTypeInfoByType(fromType, context);
        var paramImplList = argTypes.Select(x => x == null ? null : new SimpleParamImpl(x)).ToArray();
        var outerMethods = context.AssemblyContainer.GetMatchingMethods(methodName.MemberName, type, paramImplList);
        var innerMethods = context.ThisModuleAssemblyContainer.GetMatchingMethods(methodName.MemberName, type, paramImplList);
        var methods = outerMethods.Concat(innerMethods).ToArray();
        if (methods.Length > 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.CannotInferenceMethod(type.Alias, methods.First().Alias), node, null, null));
            context.PrevInferredType = null;
        }
        else if (methods.Length < 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.MethodNotFound(type.Alias, methodName.MemberName), node, null, null));
            context.PrevInferredType = null;
        }
        else
        {
            //TODO: generic method create
            var method = methods.Single();
            context.PrevInferredType = method.MethodInfo.ReturnType;
            var newCall = new CallNode(node.From, node.MethodName, node.Args.ToList()) { Symbol = method.MethodInfo };
            Replace(node, newCall);
        }
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitConstructor(ConstructorCallNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Type, context);
        var ctorType = context.PrevInferredType;
        var argTypes = GetArgTypes(node.Args, context);
        
        if (ctorType == null)
        {
            context.PrevInferredType = null;
            return VisitResult.SkipChildren;
        }

        var typeInfo = GetTypeInfoByType(ctorType, context);
        var parameters = argTypes.Select(x => x == null ? null : new SimpleParamImpl(x)).ToArray();
        var ctor = context.AssemblyContainer.GetMatchingConstructors(typeInfo, parameters);
        var innerCtor = context.ThisModuleAssemblyContainer.GetMatchingConstructors(typeInfo, parameters);
        var allCtorList = innerCtor.Concat(ctor).ToList();
        
        context.PrevInferredType = ctorType;
        if (allCtorList.Count > 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.CannotInferenceConstructor(typeInfo.Alias), node, null, null));
            return VisitResult.SkipChildren;
        }
        if (allCtorList.Count < 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.ConstructorNotFound(typeInfo.Alias), node, null, null));
            return VisitResult.SkipChildren;
        }

        var constructorInfo = allCtorList.Single();
        var newNode = new ConstructorCallNode(node.Type, node.Args) { Symbol = constructorInfo.ConstructorInfo };
        Replace(node, newNode);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitCast(CastNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.ToType, context);
        context.PrevInferredType = null;
        VisitInternal(node.Inner, context);
        var fromTypeCast = context.PrevInferredType;
        if (fromTypeCast == null)
        {
            return VisitResult.SkipChildren;
        }

        var newCast = new CastNode(node.ToType, node.Inner) { FromType = context.PrevInferredType };
        Replace(node, newCast);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitInternal(NodeBase node, TypeInferenceInnerContext context)
    {
        context.PrevInferredType = null;
        return base.VisitInternal(node, context);
    }

    protected override VisitResult VisitPlus(PlusNode node, TypeInferenceInnerContext context) => 
        VisitBinaryArithmeticResultType(node, "plus", context);

    protected override VisitResult VisitMinus(MinusNode node, TypeInferenceInnerContext context) => 
        VisitBinaryArithmeticResultType(node, "minus", context);

    protected override VisitResult VisitMultiply(MultiplyNode node, TypeInferenceInnerContext context) =>
        VisitBinaryArithmeticResultType(node, "multiply", context);

    protected override VisitResult VisitDivide(DivideNode node, TypeInferenceInnerContext context) =>
        VisitBinaryArithmeticResultType(node, "divide", context);

    protected override VisitResult VisitModulo(ModuloNode node, TypeInferenceInnerContext context) =>
        VisitBinaryArithmeticResultType(node, "modulo", context);

    protected override VisitResult VisitPrefixIncrement(PrefixIncrementNode node, TypeInferenceInnerContext context) =>
        VisitUnaryArithmeticResultType(node, "increment", context);

    protected override VisitResult VisitPostfixIncrement(PostfixIncrementNode node, TypeInferenceInnerContext context) =>
        VisitUnaryArithmeticResultType(node, "increment", context);

    protected override VisitResult VisitPostfixDecrement(PostfixDecrementNode node, TypeInferenceInnerContext context) =>
        VisitUnaryArithmeticResultType(node, "decrement", context);

    protected override VisitResult VisitPrefixDecrement(PrefixDecrementNode node, TypeInferenceInnerContext context) =>
        VisitUnaryArithmeticResultType(node, "decrement", context);

    protected override VisitResult VisitUnaryMinus(UnaryMinusNode node, TypeInferenceInnerContext context) =>
        VisitUnaryArithmeticResultType(node, "minus", context);

    protected override VisitResult VisitNot(NotNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node, context);
        if (context.PrevInferredType == typeof(bool) || context.PrevInferredType == null) return VisitResult.SkipChildren;

        var typeInfo = GetTypeInfoByType(context.PrevInferredType, context);
        context.PrevInferredType = null;
        context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
            PlampSemanticsExceptions.CannotApplyUnaryOperator("negate", typeInfo.Alias), node, null, null));
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitEqual(EqualNode node, TypeInferenceInnerContext context) =>
        VisitBinaryBooleanResultType(node, "equals", context);

    protected override VisitResult VisitNotEqual(NotEqualNode node, TypeInferenceInnerContext context) =>
        VisitBinaryBooleanResultType(node, "not equals", context);

    protected override VisitResult VisitLessOrEqual(LessOrEqualNode node, TypeInferenceInnerContext context) =>
        VisitBinaryBooleanResultType(node, "lesser or equal", context);

    protected override VisitResult VisitGreaterOrEquals(GreaterOrEqualNode node, TypeInferenceInnerContext context) =>
        VisitBinaryBooleanResultType(node, "greater or equal", context);

    protected override VisitResult VisitLess(LessNode node, TypeInferenceInnerContext context) =>
        VisitBinaryBooleanResultType(node, "lesser", context);

    protected override VisitResult VisitGreater(GreaterNode node, TypeInferenceInnerContext context) =>
        VisitBinaryBooleanResultType(node, "greater", context);

    protected override VisitResult VisitBitwiseAnd(BitwiseAndNode node, TypeInferenceInnerContext context) =>
        VisitBinaryArithmeticResultType(node, "bitwise and", context, true);

    protected override VisitResult VisitBitwiseOr(BitwiseOrNode node, TypeInferenceInnerContext context) =>
        VisitBinaryArithmeticResultType(node, "bitwise or", context, true);

    protected override VisitResult VisitXor(XorNode node, TypeInferenceInnerContext context) =>
        VisitBinaryArithmeticResultType(node, "xor", context, true);

    protected override VisitResult VisitAnd(AndNode node, TypeInferenceInnerContext context) =>
        VisitBinaryLogicGate(node, "and", context);
    
    protected override VisitResult VisitOr(OrNode node, TypeInferenceInnerContext context) =>
        VisitBinaryLogicGate(node, "or", context);

    protected override VisitResult VisitAssign(AssignNode node, TypeInferenceInnerContext context)
    {
        VisitInternal(node.Right, context);
        var rightType = context.PrevInferredType;
        switch (node.Left)
        {
            case MemberNode member:
                if (!context.VariableTypeDict.TryGetValue(member.MemberName, out var value))
                {
                    context.Exceptions.Add(
                        context.SymbolTable.SetExceptionToNodeWithoutChildren(
                            PlampSemanticsExceptions.VariableIsNotDefinedYet(member.MemberName), node, null, null));
                }
                else if(rightType != value.Value)
                {
                    var leftAlias = value.Value == null ? "???" : GetTypeInfoByType(value.Value, context).Alias;
                    var rightAlias = rightType == null ? "???" : GetTypeInfoByType(rightType, context).Alias;
                    context.Exceptions.Add(
                        context.SymbolTable.SetExceptionToNodeWithoutChildren(
                            PlampSemanticsExceptions.AssignmentTypeMismatch(leftAlias, rightAlias), node, null, null));
                }
                break;
            case MemberAccessNode memberAccess:
                
                break;
            case VariableDefinitionNode variableDefinition:
                break;
            default:
                context.Exceptions.Add(
                    context.SymbolTable.SetExceptionToNodeWithoutChildren(
                        PlampSemanticsExceptions.InvalidAssignmentTarget(), node, null, null));
                break;
        }
        
        context.PrevInferredType = null;
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitBreak(BreakNode node, TypeInferenceInnerContext context)
        => NullNodeStub(node, context);

    protected override VisitResult VisitCondition(ConditionNode node, TypeInferenceInnerContext context)
        => NullNodeStub(node, context);

    protected override VisitResult VisitWhile(WhileNode node, TypeInferenceInnerContext context)
        => NullNodeStub(node, context);

    protected override VisitResult VisitTypeDefinition(TypeDefinitionNode node, TypeInferenceInnerContext context)
        => NullNodeStub(node, context);

    protected override VisitResult VisitEmpty(EmptyNode node, TypeInferenceInnerContext context)
        => NullNodeStub(node, context);

    /*private MethodInfo? UnwrapMemberAccessSetter(MemberAccessNode node, TypeInferenceInnerContext context, bool propertySetter = true)
    {
        //Member access can be
        //variable + fld,
        //call(in prop context) here does not meets + fld,
        //fld + fld,
        //root member access can be
        //var || arg + member
        if (node.Member is not MemberNode member)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.MemberAccessCannotAccessNotMember(), node, null, null));
            context.PrevInferredType = null;
            return VisitResult.Break;
        }

        if (node.From is MemberNode fromMember)
        {
            if (context.VariableTypeDict.TryGetValue(fromMember.MemberName, out var pair))
            {
                context.PrevInferredType = pair.Value;
            }
            //TODO: add self member access
            else
            {
                var types = GetTypeInfoList(fromMember.MemberName, 0, 0, context);
                if (types.Count > 1)
                {
                    context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                        PlampSemanticsExceptions.AmbigulousTypeName(fromMember.MemberName, context.ImportedModules),
                        fromMember, null, null));
                    return VisitResult.Break;
                }

                if (types.Count == 1)
                {
                    context.PrevInferredType = types[0].Type;
                }
            }
        }
        else if (node.From is MemberAccessNode fromMemberAccess)
        {
            UnwrapMemberAccessSetter(fromMemberAccess, context, false);
        }
        else
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.InvalidAccessTarget(), node, null, null));
            context.PrevInferredType = null;
            return VisitResult.Break;
        }
        if (context.PrevInferredType == null) return VisitResult.Break;

        var typeInfo = GetTypeInfoByType(context.PrevInferredType, context);
        
        //TODO: self props
        var props = context.AssemblyContainer.GetMatchingProperties(member.MemberName, typeInfo);
        var fields = context.AssemblyContainer.GetMatchingFields(member.MemberName, typeInfo);
    }#1#
    
    private VisitResult NullNodeStub(NodeBase node, TypeInferenceInnerContext context)
    {
        context.PrevInferredType = null;
        return VisitResult.Continue;
    }

    private VisitResult VisitBinaryLogicGate(BaseBinaryNode node, string operatorName,
        TypeInferenceInnerContext context)
    {
        VisitInternal(node.Left, context);
        var leftType = context.PrevInferredType;
        VisitInternal(node.Right, context);
        var rightType = context.PrevInferredType;

        if (leftType != typeof(bool) || rightType != typeof(bool))
        {
            var leftAlias = leftType == null ? "???" : GetTypeInfoByType(leftType, context).Alias;
            var rightAlias = rightType == null ? "???" : GetTypeInfoByType(rightType, context).Alias;
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.CannotApplyBinaryOperator(operatorName, leftAlias, rightAlias),
                node, null, null));
        }

        context.PrevInferredType = typeof(bool);
        return VisitResult.SkipChildren;
    }

    private VisitResult VisitBinaryBooleanResultType(
        BaseBinaryNode node,
        string operatorName,
        TypeInferenceInnerContext context)
    {
        VisitInternal(node.Left, context);
        var leftType = context.PrevInferredType;
        VisitInternal(node.Right, context);
        var rightType = context.PrevInferredType;

        if (leftType != rightType && leftType != null && rightType != null)
        {
            var leftInfo = GetTypeInfoByType(leftType, context);
            var rightInfo = GetTypeInfoByType(rightType, context);
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.CannotApplyBinaryOperator(operatorName, leftInfo.Alias, rightInfo.Alias),
                node, null, null));
        }

        context.PrevInferredType = typeof(bool);
        return VisitResult.SkipChildren;
    }
    
    //TODO: custom operators does not supported
    private VisitResult VisitBinaryArithmeticResultType(
        BaseBinaryNode node,
        string operatorName,
        TypeInferenceInnerContext context,
        bool bitwise = false)
    {
        VisitInternal(node.Left, context);
        var leftType = context.PrevInferredType;
        VisitInternal(node.Right, context);
        var rightType = context.PrevInferredType;
        
        if (leftType != null && rightType != null && leftType == rightType)
        {
            var leftPriority = GetArithmeticTypePriority(leftType);
            var rightPriority = GetArithmeticTypePriority(rightType);
            if (leftPriority == -1 || rightPriority == -1)
            {
                var leftTypeInfo = GetTypeInfoByType(leftType, context);
                var rightTypeInfo = GetTypeInfoByType(rightType, context);
                context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                    PlampSemanticsExceptions.CannotApplyBinaryOperator(operatorName, leftTypeInfo.Alias, rightTypeInfo.Alias),
                    node, null, null));
            }

            if (leftPriority == rightPriority && leftPriority == -1)
            {
                context.PrevInferredType = null;
            }
            else
            {
                context.PrevInferredType = leftPriority > rightPriority ? leftType : rightType;
            }
        }

        if (leftType != null && rightType != null && leftType != rightType)
        {
            context.PrevInferredType = null;
        }

        if (leftType != null && rightType == null)
        {
            context.PrevInferredType = leftType;
        }

        if (leftType == null && rightType != null)
        {
            context.PrevInferredType = rightType;
        }
        else
        {
            context.PrevInferredType = null;
        }
        
        return VisitResult.SkipChildren;

        int GetArithmeticTypePriority(Type type)
        {
            if (type == typeof(double) && !bitwise) return 10;
            if (type == typeof(float) && !bitwise) return 9;
            if (type == typeof(ulong)) return 8;
            if (type == typeof(long)) return 7;
            if (type == typeof(uint)) return 6;
            if (type == typeof(int)) return 5;
            if (type == typeof(ushort)) return 4;
            if (type == typeof(short)) return 3;
            if (type == typeof(byte)) return 2;
            if (type == typeof(sbyte)) return 1;
            if (type == typeof(bool) && bitwise) return 0;
            return -1;
        }
    }

    private VisitResult VisitUnaryArithmeticResultType(BaseUnaryNode unary, string opName, TypeInferenceInnerContext context)
    {
        VisitInternal(unary.Inner, context);
        if (context.PrevInferredType == null
            || context.PrevInferredType == typeof(int)
            || context.PrevInferredType == typeof(long)
            || context.PrevInferredType == typeof(ulong)
            || context.PrevInferredType == typeof(uint)
            || context.PrevInferredType == typeof(short)
            || context.PrevInferredType == typeof(ushort)
            || context.PrevInferredType == typeof(byte)
            || context.PrevInferredType == typeof(sbyte))
        {
            return VisitResult.SkipChildren;
        }
        var allTypes = GetTypeInfoByType(context.PrevInferredType, context);
        context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
            PlampSemanticsExceptions.CannotApplyUnaryOperator(opName, allTypes.Alias), unary, null, null));
        context.PrevInferredType = null;
        return VisitResult.SkipChildren;
    }

    private bool TryGetTypeAndValidateUniqueness(TypeNode type, TypeInferenceInnerContext context, out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        if (type.TypeName is not MemberNode { MemberName: not null } member)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.TypeNameMustBeMember(), type, null, null));
            context.PrevInferredType = null;
            return false;
        }
        
        var allTypes = GetTypeInfoList(member.MemberName, type.InnerGenerics?.Count ?? 0, 0, context);
        
        if (allTypes.Count > 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.AmbigulousTypeName(member.MemberName, allTypes.Select(x => x.Module).Distinct()),
                type, null, null));
            context.PrevInferredType = null;
            return false;
        }

        if (allTypes.Count < 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.TypeNotFound(member.MemberName), type, null, null));
            return false;
        }

        typeInfo = allTypes.Single();
        return true;
    }

    private ITypeInfo GetTypeInfoByType(Type type, TypeInferenceInnerContext context)
    {
        var outerTypeInfo = context.AssemblyContainer.GetMatchingTypes(type);
        var innerTypeInfo = context.ThisModuleAssemblyContainer.GetMatchingTypes(type);
        var typeInfo = outerTypeInfo.Concat(innerTypeInfo).Single();
        return typeInfo;
    }
        
    private List<ITypeInfo> GetTypeInfoList(string typeName, int genericCount, int arrayDimensions, TypeInferenceInnerContext context)
    {
        var innerTypes = context.ThisModuleAssemblyContainer.GetMatchingTypes(typeName, genericCount);
        var outerTypes = context.AssemblyContainer
            .GetMatchingTypes(typeName, genericCount, arrayDimensions)
            .Where(x => context.ImportedModules.Contains(x.Module));
        
        var allTypes = innerTypes.Concat(outerTypes).ToList();
        return allTypes;
    }
    
    private List<Type?> GetArgTypes(IReadOnlyList<NodeBase?> args, TypeInferenceInnerContext context)
    {
        List<Type?> argTypes = [];
        foreach (var arg in args)
        {
            if (arg == null)
            {
                argTypes.Add(null);
                context.PrevInferredType = null;
                continue;
            }

            VisitInternal(arg, context);
            argTypes.Add(context.PrevInferredType);
            context.PrevInferredType = null;
        }

        return argTypes;
    }

    private class SimpleParamImpl(Type paramType) : ParameterInfo
    {
        public override Type ParameterType { get; } = paramType;
    }
}*/