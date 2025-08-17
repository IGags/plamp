using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;

public class MemberNameUniquenessValidator : BaseValidator<PreCreationContext, MemberNameUniquenessValidatorInnerContext>
{
    protected override MemberNameUniquenessValidatorInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext,
        MemberNameUniquenessValidatorInnerContext innerContext) =>
        outerContext;

    protected override VisitResult PreVisitMember(MemberNode node, MemberNameUniquenessValidatorInnerContext context, NodeBase? parent)
    {
        if (parent is not FuncNode func) return VisitResult.SkipChildren;
        
        if (!context.Members.TryGetValue(node.MemberName, out var members))
        {
            members = [];
            context.Members.Add(node.MemberName, members);
        }
        members.Add(func);

        return VisitResult.SkipChildren;
    }


    protected override VisitResult PostVisitRoot(RootNode node, MemberNameUniquenessValidatorInnerContext context, NodeBase? parent)
    {
        var record = PlampExceptionInfo.DuplicateMemberNameInModule();
        foreach (var member in context.Members.Values.Where(x => x.Count > 1).SelectMany(x => x))
        {
            SetExceptionToSymbol(member, record, context);
        }

        return VisitResult.Break;
    }
}