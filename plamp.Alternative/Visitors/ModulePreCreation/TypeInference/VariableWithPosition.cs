using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Variable;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public record VariableWithPosition(VariableDefinitionNode Variable, ScopeLocation InScopePositionList);