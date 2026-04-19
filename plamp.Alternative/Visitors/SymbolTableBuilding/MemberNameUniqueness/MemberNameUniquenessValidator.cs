using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.MemberNameUniqueness;

public class MemberNameUniquenessValidator : BaseValidator<SymbolTableBuildingContext, MemberNameUniquenessValidatorInnerContext>
{
    protected override VisitorGuard Guard => VisitorGuard.TopLevel;

    protected override MemberNameUniquenessValidatorInnerContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(SymbolTableBuildingContext outerContext,
        MemberNameUniquenessValidatorInnerContext innerContext) =>
        outerContext;

    protected override VisitResult PreVisitFuncName(
        FuncNameNode node,
        MemberNameUniquenessValidatorInnerContext context,
        NodeBase? parent)
    {
        if (!context.Members.TryAdd(node.Value, [node]))
        {
            context.Members[node.Value].Add(node);
        }
        
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitTypedefName(
        TypedefNameNode node,
        MemberNameUniquenessValidatorInnerContext context,
        NodeBase? parent)
    {
        if (!context.Members.TryAdd(node.Value, [node]))
        {
            context.Members[node.Value].Add(node);
        }
        
        return VisitResult.SkipChildren;
    }


    protected override VisitResult PostVisitRoot(RootNode node, MemberNameUniquenessValidatorInnerContext context, NodeBase? parent)
    {
        var record = PlampExceptionInfo.DuplicateMemberNameInModule();
        foreach (var duplicates in context.Members)
        {
            if (duplicates.Value.Count == 1) continue;
            
            foreach (var duplicate in duplicates.Value)
            {
                SetExceptionToSymbol(duplicate, record, context);
            }
        }

        return VisitResult.Break;
    }
}