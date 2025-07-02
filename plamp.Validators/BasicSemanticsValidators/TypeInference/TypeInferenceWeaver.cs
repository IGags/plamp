using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Validators.BasicSemanticsValidators.TypeInference;

public class TypeInferenceWeaver : BaseWeaver<TypeInferenceContext, TypeInferenceInnerContext>
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
            context.VariableTypeDict[parameterName.MemberName] = context.PrevInferredType;
        }
        
        return VisitResult.SkipChildren;
    }

    protected override VisitResult VisitType(TypeNode node, TypeInferenceInnerContext context)
    {
        if (node.TypeName is not MemberNode { MemberName: not null } member)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(PlampSemanticsExceptions.TypeNameMustBeMember(), node, null, null));
            context.PrevInferredType = null;
            return VisitResult.SkipChildren;
        }

        var innerTypes = context.ThisModuleAssemblyContainer.GetMatchingTypes(member.MemberName, node.InnerGenerics?.Count ?? 0);
        var outerTypes = context.AssemblyContainer.GetMatchingTypes(member.MemberName, node.InnerGenerics?.Count ?? 0).Where(x => context.ImportedModules.Contains(x.Module));
        var allTypes = innerTypes.Concat(outerTypes).ToList();
        
        if (allTypes.Count > 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.AmbigulousTypeName(member.MemberName, allTypes.Select(x => x.Module).Distinct()),
                node, null, null));
            context.PrevInferredType = null;
            return VisitResult.SkipChildren;
        }

        if (allTypes.Count < 1)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNodeWithoutChildren(
                PlampSemanticsExceptions.TypeNotFound(member.MemberName), node, null, null));
            return VisitResult.SkipChildren;
        }

        var inferredType = allTypes[0];
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
            var type = inferredType.Type.MakeGenericType(genericTypes.ToArray()!);
            context.PrevInferredType = type;
            
        }
        else
        {
            context.PrevInferredType = inferredType.Type;
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
        
        List<Type?> argTypes = [];
        foreach (var arg in node.Args)
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

        if (fromType == null)
        {
            context.PrevInferredType = null;
            return VisitResult.SkipChildren;
        }

        var outerTypeInfo = context.AssemblyContainer.GetMatchingTypes(fromType);
        var innerTypeInfo = context.ThisModuleAssemblyContainer.GetMatchingTypes(fromType);
        var type = outerTypeInfo.Concat(innerTypeInfo).Single();

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

    protected override VisitResult VisitConstructor(ConstructorCallNode callNode, TypeInferenceInnerContext context)
    {
        
    }

    private class SimpleParamImpl(Type paramType) : ParameterInfo
    {
        public override Type ParameterType { get; } = paramType;
    }
}