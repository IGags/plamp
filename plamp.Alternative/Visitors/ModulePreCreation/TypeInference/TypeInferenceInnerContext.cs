using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public class TypeInferenceInnerContext(BaseVisitorContext other) : PreCreationContext(other)
{
    public Type? InnerExpressionType { get; set; }
    
    public DefNode? CurrentFunc { get; set; }

    public Dictionary<string, VariableDefinitionNode> VariableDefinitions { get; } = [];
    public Dictionary<string, ParameterNode> Arguments { get; } = [];
    public List<string> CurrentScopeDefinitions { get; } = [];
}