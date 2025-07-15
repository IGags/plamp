using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;

namespace plamp.Abstractions.AstManipulation.Modification;

public abstract class BaseWeaver<TOuterContext, TInnerContext> 
    : BaseVisitor<TInnerContext>, IWeaver<TOuterContext> 
    where TOuterContext : BaseVisitorContext 
    where TInnerContext : BaseVisitorContext
{
    protected Dictionary<NodeBase, NodeBase> ReplacementDict { get; } = [];
    protected Dictionary<NodeBase, KeyValuePair<FilePosition, FilePosition>> SymbolReplaceDict { get; } = [];
    
    public virtual TOuterContext WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitInternal(ast, innerContext);
        var result = MapInnerToOuter(innerContext, context);
        ProceedNodeReplacement(ast, context);
        return result;
    }

    protected sealed override VisitResult VisitInternal(NodeBase node, TInnerContext context)
    {
        return base.VisitInternal(node, context);
    }

    protected abstract TInnerContext CreateInnerContext(TOuterContext context);

    protected abstract TOuterContext MapInnerToOuter(TInnerContext innerContext, TOuterContext outerContext);

    protected virtual void Replace(NodeBase from, NodeBase to, TInnerContext context)
    {
        if(from.GetType() == typeof(RootNode)) throw new ArgumentException("Cannot replace root node, check visitor code");
        if (!context.SymbolTable.TryGetSymbol(from, out var pair))
            throw new ArgumentException(
                "Symbol does not exists in table, please check parser and tree construction logic");
        SymbolReplaceDict.Add(from, pair);
        ReplacementDict.Add(from, to);
    }

    private void ProceedNodeReplacement(NodeBase ast, TOuterContext context)
    {
        var nodeChildren = ast.Visit();
        ProceedRecursive(nodeChildren, ast);
        
        void ProceedRecursive(IEnumerable<NodeBase> children, NodeBase parent)
        {
            foreach (var child in children.ToList())
            {
                if (
                    ReplacementDict.TryGetValue(child, out var replacement)
                    && SymbolReplaceDict.TryGetValue(child, out var pair))
                {
                    parent.ReplaceChild(child, replacement);
                    context.SymbolTable.AddSymbol(replacement, pair.Key, pair.Value);
                    continue;
                }

                var innerChildren = child.Visit();
                ProceedRecursive(innerChildren, child);
            }
        }
    }
}