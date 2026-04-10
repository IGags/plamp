using System.Linq;
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
        context.Funcs.Add(node);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitTypedefName(
        TypedefNameNode node,
        MemberNameUniquenessValidatorInnerContext context,
        NodeBase? parent)
    {
        context.Types.Add(node);
        return VisitResult.SkipChildren;
    }


    protected override VisitResult PostVisitRoot(RootNode node, MemberNameUniquenessValidatorInnerContext context, NodeBase? parent)
    {
        var typeNames = context.Types.Select(x => x.Value).ToHashSet();
        typeNames.IntersectWith(context.Funcs.Select(x => x.Value));

        if (typeNames.Count == 0) return VisitResult.Break;

        var record = PlampExceptionInfo.DuplicateMemberNameInModule();
        foreach (var name in typeNames)
        {
            var matchFuncs = context.Funcs.Where(x => x.Value == name);
            var matchTypes = context.Types.Where(x => x.Value == name);
            foreach (var fn in matchFuncs)
            {
                SetExceptionToSymbol(fn, record, context);
            }

            foreach (var typ in matchTypes)
            {
                SetExceptionToSymbol(typ, record, context);
            }
        }

        return VisitResult.Break;
    }
}