using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.DuplicateArgumentName;

public class DuplicateArgumentNameValidator : BaseValidator<PreCreationContext, DuplicateArgumentNameContext>
{
    protected override DuplicateArgumentNameContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, DuplicateArgumentNameContext innerContext) => innerContext;

    protected override VisitResult PreVisitFunction(FuncNode node, DuplicateArgumentNameContext context, NodeBase? parent)
    {
        var paramGroups = node.ParameterList.GroupBy(x => x.Name.Value);
        var record = PlampExceptionInfo.DuplicateParameterName();
        foreach (var parameter in paramGroups.Where(x => x.Count() > 1).SelectMany(x => x))
        {
            SetExceptionToSymbol(parameter, record, context);
        }

        return VisitResult.SkipChildren;
    }
}