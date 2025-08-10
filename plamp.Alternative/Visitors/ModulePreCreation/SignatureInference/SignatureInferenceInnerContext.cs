using System.Collections.Generic;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;

public class SignatureInferenceInnerContext : PreCreationContext
{
    public List<string> MemberSet { get; } = [];
    
    public SignatureInferenceInnerContext(BaseVisitorContext other) : base(other)
    {
    }
}