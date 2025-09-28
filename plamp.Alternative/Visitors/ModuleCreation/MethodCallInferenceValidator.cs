using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class MethodCallInferenceValidator : BaseValidator<CreationContext, CreationContext>
{
    //TODO: Сломается при перегрузках метода. Нужно чинить.
    protected override VisitResult PreVisitCall(CallNode node, CreationContext context, NodeBase? parent)
    {
        var info = node.Symbol;
        var fromContext = context.Methods.FirstOrDefault(x => x.Name == node.Name.Value);
        if (fromContext != null) info = fromContext;
        if(info != null) node.SetInfo(info);
        return VisitResult.Continue;
    }

    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;
}