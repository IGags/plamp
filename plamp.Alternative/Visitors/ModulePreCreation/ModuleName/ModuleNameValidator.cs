using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.ModuleName;

public class ModuleNameValidator : BaseValidator<PreCreationContext, ModuleNameValidatorContext>
{
    protected override ModuleNameValidatorContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, ModuleNameValidatorContext context) => new(context);

    protected override VisitResult VisitRoot(RootNode node, ModuleNameValidatorContext context)
    {
        if (node.ModuleName == null)
        {
            var record = PlampExceptionInfo.ModuleMustHaveName();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            return VisitResult.Break;
        }
        context.ModuleName = node.ModuleName?.ModuleName;
        if(node.ModuleName == null 
           || !context.Members.TryGetValue(node.ModuleName.ModuleName, out var members)) return VisitResult.Break;
        
        var exceptionRecord = PlampExceptionInfo.MemberCannotHaveSameNameAsDeclaringModule();
        foreach (var member in members)
        {
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(member, exceptionRecord, context.FileName));
        }

        return VisitResult.Break;
    }

    protected override VisitResult VisitDef(DefNode node, ModuleNameValidatorContext context)
    {
        if (!context.Members.TryGetValue(node.Name.MemberName, out var members))
        {
            members = [];
            context.Members.Add(node.Name.MemberName, members);
        }
        members.Add(node);
        return VisitResult.SkipChildren;
    }
}