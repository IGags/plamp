using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class FuncCreatorValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, CreationContext context, NodeBase? parent)
    {
        var signature = node.ParameterList.Select(x => x.Type.TypeInfo).ToArray();
        var signatureTypes = new List<Type>();
        foreach (var typeRef in signature)
        {
            var clrType = typeRef?.AsType();
            if (clrType == null) return VisitResult.SkipChildren;
            signatureTypes.Add(clrType);
        }
        
        var retTypeInfo = node.ReturnType.TypeInfo;
        var retType = retTypeInfo?.AsType();
        if (retType == null) return VisitResult.SkipChildren;
        
        var methodBuilder = context.ModuleBuilder.DefineGlobalMethod(
            node.FuncName.Value,
            MethodAttributes.Static | MethodAttributes.Final | MethodAttributes.Public,
            CallingConventions.Standard,
            retType,
            signatureTypes.ToArray());
        node.Func = methodBuilder;
        return VisitResult.SkipChildren;
    }

    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;
}