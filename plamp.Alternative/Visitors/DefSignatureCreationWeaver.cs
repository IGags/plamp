using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.AstExtensions;

namespace plamp.Alternative.Visitors;

public class DefSignatureCreationWeaver : BaseExtendedWeaver<DefSignatureCreationContext, DefSignatureCreationContextInner, DefSignatureCreationResult>
{
    protected override DefSignatureCreationContextInner CreateInnerContext(DefSignatureCreationContext context) => new (context.ModuleBuilder);

    protected override DefSignatureCreationResult CreateWeaveResult(DefSignatureCreationContextInner innerContext,
        DefSignatureCreationContext outerContext)
    {
        return new(innerContext.Methods);
    }

    protected override VisitResult VisitDef(DefNode node, DefSignatureCreationContextInner context)
    {
        Type?[] signature = node.ParameterList.Select(x => x.Type.Symbol).ToArray();
        if (signature.Any(x => x == null)) return VisitResult.SkipChildren;
        var retType = node.ReturnType.Symbol;
        if(retType == null) return VisitResult.SkipChildren;
        var methodBuilder = context.ModuleBuilder.DefineGlobalMethod(
            node.Name.MemberName,
            MethodAttributes.Static | MethodAttributes.Final | MethodAttributes.Public,
            CallingConventions.Standard,
            retType,
            signature!);
        context.Methods.Add(methodBuilder);
        return VisitResult.SkipChildren;
    }
}

public record DefSignatureCreationContext(ModuleBuilder ModuleBuilder);

public record DefSignatureCreationContextInner(ModuleBuilder ModuleBuilder)
{
    public List<MethodBuilder> Methods { get; } = [];
}

public record DefSignatureCreationResult(List<MethodBuilder> Methods);