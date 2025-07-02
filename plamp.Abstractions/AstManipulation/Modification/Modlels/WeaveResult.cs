using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Modification.Modlels;

public record WeaveResult
{
    public IReadOnlyDictionary<NodeBase, NodeBase> NodeDiffDictionary { get; init; } = new Dictionary<NodeBase, NodeBase>();
    
    public IReadOnlyList<PlampException> Exceptions { get; init; } = new List<PlampException>();
};