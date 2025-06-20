using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Modification.Modlels;

public record WeaveResult
{
    public IReadOnlyDictionary<NodeBase, NodeBase> NodeDiffDictionary { get; init; } = [];
};