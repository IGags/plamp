using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.AstExtensions;

public abstract class BaseExtendedWeaver<TContext, TInnerContext, TReturn> : BaseWeaver<TContext, TInnerContext, TReturn>
{
    protected override VisitResult VisitDefault(NodeBase node, TInnerContext context)
    {
        switch (node)
        {
            case RootNode root:
                return VisitRoot(root, context);
            case ImportNode import:
                return VisitImport(import, context);
            case ImportItemNode importItem:
                return VisitImportItem(importItem, context);
            case ModuleDefinitionNode moduleDefinition:
                return VisitModuleDefinition(moduleDefinition, context);
        }

        return VisitResult.SkipChildren;
    }

    protected virtual VisitResult VisitRoot(RootNode node, TInnerContext context)
    {
        return VisitResult.Continue;
    }

    protected virtual VisitResult VisitImport(ImportNode node, TInnerContext context)
    {
        return VisitResult.Continue;
    }

    protected virtual VisitResult VisitImportItem(ImportItemNode node, TInnerContext context)
    {
        return VisitResult.Continue;
    }

    protected virtual VisitResult VisitModuleDefinition(ModuleDefinitionNode definition, TInnerContext context)
    {
        return VisitResult.Continue;
    }
}