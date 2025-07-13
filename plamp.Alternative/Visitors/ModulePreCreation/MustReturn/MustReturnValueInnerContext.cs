using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MustReturn;

public class MustReturnValueInnerContext(BaseVisitorContext outer) : PreCreationContext(outer)
{
    public bool LexicalScopeAlwaysReturns { get; set; }
}