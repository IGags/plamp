using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.AstExtensions;

public abstract class BaseExtendedValidator<TOuterContext, TInnerContext> : BaseValidator<TOuterContext, TInnerContext>
{
    protected override VisitResult VisitDefault(NodeBase node, TInnerContext context)
    {
        switch (node)
        {
            case Abstractions.Ast.Node.RootNode root:
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

    protected virtual VisitResult VisitRoot(Abstractions.Ast.Node.RootNode node, TInnerContext context)
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