using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public record VariableWithPosition(VariableDefinitionNode Variable, ScopeLocation InScopePositionList);