using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class DefSignatureCreationValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, CreationContext context, NodeBase? parent)
    {
        var signature = node.ParameterList.Select(x => x.Type.Symbol).ToArray();
        if (signature.Any(x => x == null)) return VisitResult.SkipChildren;
        var retType = node.ReturnType?.Symbol;
        if(retType == null) return VisitResult.SkipChildren;
        var methodBuilder = context.ModuleBuilder.DefineGlobalMethod(
            node.FuncName.Value,
            MethodAttributes.Static | MethodAttributes.Final | MethodAttributes.Public,
            CallingConventions.Standard,
            retType,
            signature!);
        context.Methods.Add(methodBuilder);
        return VisitResult.SkipChildren;
    }

    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;
}