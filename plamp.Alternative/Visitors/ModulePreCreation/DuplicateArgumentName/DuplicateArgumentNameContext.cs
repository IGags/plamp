using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.DuplicateArgumentName;

public class DuplicateArgumentNameContext(BaseVisitorContext other) : PreCreationContext(other);