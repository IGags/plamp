using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class MethodCallInferenceValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override VisitResult VisitCall(CallNode node, CreationContext context)
    {
        var info = TypeResolveHelper.TryGetIntrinsic(node.MethodName.MemberName);
        var fromContext = context.Methods.FirstOrDefault(x => x.Name == node.MethodName.MemberName);
        if (fromContext != null) info = fromContext;
        if(info != null) node.SetInfo(info);
        return VisitResult.Continue;
    }

    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;
}