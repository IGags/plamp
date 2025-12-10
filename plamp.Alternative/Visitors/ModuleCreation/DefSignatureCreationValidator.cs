using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class DefSignatureCreationValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, CreationContext context, NodeBase? parent)
    {
        var signature = node.ParameterList.Select(x => x.Type.TypedefRef).ToArray();
        var signatureTypes = new List<Type>();
        foreach (var typeRef in signature)
        {
            var info = typeRef?.GetDefinitionInfo();
            if (info?.ClrType == null) return VisitResult.SkipChildren;
            signatureTypes.Add(info.ClrType);
        }
        
        var retType = node.ReturnType.TypedefRef;
        var typeInfo = retType?.GetDefinitionInfo();
        if (typeInfo?.ClrType == null) return VisitResult.SkipChildren;
        
        var methodBuilder = context.ModuleBuilder.DefineGlobalMethod(
            node.FuncName.Value,
            MethodAttributes.Static | MethodAttributes.Final | MethodAttributes.Public,
            CallingConventions.Standard,
            typeInfo.ClrType,
            signatureTypes.ToArray());
        var fnRef = context.SymbolTable.GetMatchingFunction(node.FuncName.Value, signature);
        fnRef?.GetDefinitionInfo().SetClrMethod(methodBuilder);
        return VisitResult.SkipChildren;
    }

    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;
}