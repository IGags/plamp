using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;

public class MemberNameUniquenessValidator : BaseValidator<PreCreationContext, MemberNameUniquenessValidatorInnerContext>
{
    protected override MemberNameUniquenessValidatorInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext,
        MemberNameUniquenessValidatorInnerContext innerContext) =>
        outerContext;

    protected override VisitResult PreVisitFuncName(
        FuncNameNode node, 
        MemberNameUniquenessValidatorInnerContext context, 
        NodeBase? parent)
    {
        if (parent is not FuncNode func) return VisitResult.SkipChildren;
        AddMemberToContext(node.Value, func, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitTypedefName(
        TypedefNameNode node,
        MemberNameUniquenessValidatorInnerContext context,
        NodeBase? parent)
    {
        if (parent is not TypedefNode typedef) return VisitResult.SkipChildren;
        AddMemberToContext(node.Value, typedef, context);
        return VisitResult.SkipChildren;
    }

    private static void AddMemberToContext(string memberName, NodeBase member,
        MemberNameUniquenessValidatorInnerContext context)
    {
        if (!context.Members.TryGetValue(memberName, out var members))
        {
            members = [];
            context.Members.Add(memberName, members);
        }
        members.Add(member);
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