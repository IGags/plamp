using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Extensions;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Ast.NodeComparers.Common;

namespace plamp.Abstractions.Ast.NodeComparers;

public class RecursiveComparer : IEqualityComparer<NodeBase>
{
    public bool Equals(NodeBase x, NodeBase y)
    {
        var comparisionStack = new Stack<KeyValuePair<NodeBase, NodeBase>>();
        comparisionStack.Push(new (x, y));
        while (comparisionStack.Count > 0)
        {
            var kvp = comparisionStack.Pop();
            var res = CompareInner(kvp.Key, kvp.Value, ref comparisionStack);
            if(!res) return false;
        }

        return true;
    }

    [Obsolete("DO NOT USE THIS")]
    public int GetHashCode(NodeBase obj)
    {
        throw new NotImplementedException();
    }

    private bool CompareInner(NodeBase first, NodeBase second, 
        //For readability
        ref Stack<KeyValuePair<NodeBase, NodeBase>> comparisionStack)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first == null || second == null) return false;
        if (first.GetType() == second.GetType())
        {
            switch (first)
            {
                //TODO: not optimal, better pre-create comparer
                case BaseBinaryNode node:
                    var binaryComparer = new BinaryComparer();
                    if (!binaryComparer.Equals(node, (BaseBinaryNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case BreakNode node:
                    var breakComparer = new BreakComparer();
                    if (!breakComparer.Equals(node, (BreakNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case CallNode node:
                    var callComparer = new CallComparer();
                    if (!callComparer.Equals(node, (CallNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case CastNode node:
                    var castComparer = new CastComparer();
                    if (!castComparer.Equals(node, (CastNode)second)) return false;
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
                case LiteralNode node:
                    var constComparer = new LiteralComparer();
                    if (!constComparer.Equals(node, (LiteralNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case ConstructorCallNode node:
                    var constructorComparer = new ConstructorComparer();
                    if (!constructorComparer.Equals(node, (ConstructorCallNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case ContinueNode node:
                    var continueComparer = new ContinueComparer();
                    if (!continueComparer.Equals(node, (ContinueNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case DefNode node:
                    var defComparer = new DefComparer();
                    if (!defComparer.Equals(node, (DefNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case EmptyNode node:
                    var emptyComparer = new EmptyComparer();
                    if (!emptyComparer.Equals(node, (EmptyNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
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
                case MemberAccessNode node:
                    var memberAccessComparer = new MemberAccessComparer();
                    if (!memberAccessComparer.Equals(node, (MemberAccessNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case MemberNode node:
                    var memberComparer = new MemberComparer();
                    if (!memberComparer.Equals(node, (MemberNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case ParameterNode node:
                    var parameterComparer = new ParameterComparer();
                    if (!parameterComparer.Equals(node, (ParameterNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case ReturnNode node:
                    var returnComparer = new ReturnComparer();
                    if (!returnComparer.Equals(node, (ReturnNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case TypeNode node:
                    var typeComparer = new TypeComparer();
                    if (!typeComparer.Equals(node, (TypeNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case BaseUnaryNode node:
                    var unaryComparer = new UnaryComparer();
                    if (!unaryComparer.Equals(node, (BaseUnaryNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case UseNode node:
                    var useComparer = new UseComparer();
                    if (!useComparer.Equals(node, (UseNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case VariableDefinitionNode node:
                    var variableDefinitionComparer = new VariableDefinitionComparer();
                    if (!variableDefinitionComparer.Equals(node, (VariableDefinitionNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case WhileNode node:
                    var whileComparer = new WhileComparer();
                    if (!whileComparer.Equals(node, (WhileNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case BodyNode node:
                    var bodyComparer = new BodyComparer();
                    if(!bodyComparer.Equals(node, (BodyNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
                case TypeDefinitionNode node:
                    var typeDefinitionComparer = new TypeDefinitionComparer();
                    if (!typeDefinitionComparer.Equals(node, (TypeDefinitionNode)second)) return false;
                    PushChildren(comparisionStack, node, second);
                    return true;
            }
        }
        return false;
    }

    private void PushChildren(Stack<KeyValuePair<NodeBase, NodeBase>> comparisionStack, NodeBase first, NodeBase second)
    {
        var kvpArray = MakeVisitPair(first, second);
        foreach (var pair in kvpArray)
        {
            comparisionStack.Push(pair);
        }
    }
    
    private KeyValuePair<NodeBase, NodeBase>[] MakeVisitPair(NodeBase first, NodeBase second)
    {
        var firstEnum = first.Visit().ToArray();
        var secondEnum = second.Visit().ToArray();
        
        Debug.Assert(firstEnum.Length == secondEnum.Length);
        var res = new KeyValuePair<NodeBase, NodeBase>[firstEnum.Length];
        for (var i = 0; i < firstEnum.Length; i++)
        {
            res[i] = new(firstEnum[i], secondEnum[i]);
        }

        return res;
    }
}