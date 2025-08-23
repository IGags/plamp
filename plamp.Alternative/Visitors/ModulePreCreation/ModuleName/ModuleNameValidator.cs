using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.ModuleName;

public class ModuleNameValidator : BaseValidator<PreCreationContext, ModuleNameValidatorContext>
{
    protected override ModuleNameValidatorContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, ModuleNameValidatorContext context) => new(context);

    protected override VisitResult PreVisitRoot(RootNode node, ModuleNameValidatorContext context, NodeBase? parent)
    {
        if (node.ModuleName == null)
        {
            var record = PlampExceptionInfo.ModuleMustHaveName();
            context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(node, record, context.FileName));
            return VisitResult.Break;
        }
        
        context.ModuleName = node.ModuleName?.ModuleName;
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitFuncName(FuncNameNode node, ModuleNameValidatorContext context, NodeBase? parent)
    {
        if (parent is null || !node.Value.Equals(context.ModuleName)) return VisitResult.SkipChildren;

        var record = PlampExceptionInfo.MemberCannotHaveSameNameAsDeclaringModule();
        SetExceptionToSymbol(parent, record, context);

        return VisitResult.SkipChildren;
    }
}