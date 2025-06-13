using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Abstractions.Extensions.Ast.Node;
using ConditionNode = plamp.Abstractions.Extensions.Ast.Node.ConditionNode;

namespace plamp.Abstractions.Extensions.Ast.Comparers;

public class ExtendedRecursiveComparer : RecursiveComparer
{
    public override bool CompareCustom(NodeBase first, NodeBase second, Stack<KeyValuePair<NodeBase, NodeBase>> comparisionStack)
    {
        switch (first)
        {
            case ForNode node:
                var forComparer = new ForComparer();
                if (!forComparer.Equals(node, (ForNode)second)) return false;
                PushChildren(comparisionStack, node, second);
                return true;
            case ForeachNode node:
                var foreachComparer = new ForeachComparer();
                if (!foreachComparer.Equals(node, (ForeachNode)second)) return false;
                PushChildren(comparisionStack, node, second);
                return true;
            case IndexerNode node:
                var indexerComparer = new IndexerComparer();
                if (!indexerComparer.Equals(node, (IndexerNode)second)) return false;
                PushChildren(comparisionStack, node, second);
                return true;
            case ClauseNode node:
                var clauseComparer = new ClauseComparer();
                if (!clauseComparer.Equals(node, (ClauseNode)second)) return false;
                PushChildren(comparisionStack, node, second);
                return true;
            case ConditionNode node:
                var conditionComparer = new ConditionComparer();
                if (!conditionComparer.Equals(node, (ConditionNode)second)) return false;
                PushChildren(comparisionStack, node, second);
                return true;
            default: return false;
        }
    }
}