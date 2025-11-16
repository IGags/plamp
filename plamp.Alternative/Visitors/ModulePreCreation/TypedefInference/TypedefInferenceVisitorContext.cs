using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypedefInference;

public class TypedefInferenceVisitorContext : PreCreationContext
{
    public TypedefInferenceVisitorContext(BaseVisitorContext other) : base(other)
    {
    }
}