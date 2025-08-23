using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;

public class SignatureInferenceInnerContext : PreCreationContext
{
    public SignatureInferenceInnerContext(BaseVisitorContext other) : base(other)
    {
    }
}