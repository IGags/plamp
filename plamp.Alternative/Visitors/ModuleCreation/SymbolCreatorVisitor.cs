using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class SymbolCreatorVisitor : BaseValidator<CreationContext, CreationContext>
{
    protected override CreationContext CreateInnerContext(CreationContext context)
    {
        throw new System.NotImplementedException();
    }

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext)
    {
        throw new System.NotImplementedException();
    }
}