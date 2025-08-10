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
    
    public virtual TOuterContext WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitInternal(ast, innerContext);
        var result = MapInnerToOuter(innerContext, context);
        ProceedNodeReplacement(ast);
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
        {
            throw new ArgumentException("Symbol does not exists in table, please check parser and tree construction logic");
        }
        
        //Immediate symbol addition may create a memory leak, possible need create another variation of symbol table with unused nodes cleanup 
        context.SymbolTable.AddSymbol(to, pair.Key, pair.Value);
        ReplacementDict.Add(from, to);
    }

    private void ProceedNodeReplacement(NodeBase ast)
    {
        var nodeChildren = ast.Visit();
        ProceedRecursive(nodeChildren, ast);
        return;

        void ProceedRecursive(IEnumerable<NodeBase> children, NodeBase parent)
        {
            foreach (var child in children.ToList())
            {
                var innerChildren = child.Visit();
                ProceedRecursive(innerChildren, child);
                
                if (ReplacementDict.TryGetValue(child, out var replacement))
                {
                    parent.ReplaceChild(child, replacement);
                }
            }
        }
    }
}