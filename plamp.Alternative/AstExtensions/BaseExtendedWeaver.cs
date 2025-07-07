using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Alternative.AstExtensions;
using RootNode = plamp.Abstractions.Ast.Node.RootNode;

namespace plamp.Alternative.Visitors.Base;

public abstract class BaseExtendedWeaver<TContext, TInnerContext, TReturn> : BaseWeaver<TContext, TInnerContext, TReturn>
{
    protected override VisitResult VisitDefault(NodeBase node, TInnerContext context)
    {
        switch (node)
        {
            case RootNode root:
                return VisitRoot(root);
            case ImportNode import:
                return VisitImport(import);
            case ImportItemNode importItem:
                return VisitImportItem(importItem);
            case ModuleDefinitionNode moduleDefinition:
                return VisitModuleDefinition(moduleDefinition);
        }

        return VisitResult.SkipChildren;
    }

    protected virtual VisitResult VisitRoot(RootNode node)
    {
        return VisitResult.Continue;
    }

    protected virtual VisitResult VisitImport(ImportNode node)
    {
        return VisitResult.Continue;
    }

    protected virtual VisitResult VisitImportItem(ImportItemNode node)
    {
        return VisitResult.Continue;
    }

    protected virtual VisitResult VisitModuleDefinition(ModuleDefinitionNode definition)
    {
        return VisitResult.Continue;
    }
}