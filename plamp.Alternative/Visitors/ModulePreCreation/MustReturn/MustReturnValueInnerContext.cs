namespace plamp.Alternative.Visitors.ModulePreCreation.MustReturn;

public class MustReturnValueInnerContext(PreCreationContext outer) : PreCreationContext(outer)
{
    public bool LexicalScopeAlwaysReturns { get; set; }
}