using System.Linq;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;

public class MemberNameUniquenessValidator : BaseValidator<PreCreationContext, MemberNameUniquenessValidatorInnerContext>
{
    protected override MemberNameUniquenessValidatorInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext,
        MemberNameUniquenessValidatorInnerContext innerContext) =>
        outerContext;

    protected override VisitResult VisitRoot(RootNode node, MemberNameUniquenessValidatorInnerContext context)
    {
        foreach (var child in node.Visit())
        {
            switch (child)
            {
                case DefNode defNode:
                    if (!context.Members.TryGetValue(defNode.Name.MemberName, out var members))
                    {
                        members = [];
                        context.Members.Add(defNode.Name.MemberName, members);
                    }
                    members.Add(defNode);
                    break;
            }
        }

        var record = PlampExceptionInfo.DuplicateMemberNameInModule();
        foreach (var member in context.Members.Values.Where(x => x.Count > 1).SelectMany(x => x))
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(member, record, context.FileName));
        }

        return VisitResult.Break;
    }
}