using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.ModuleName;

public class ModuleNameValidator : BaseValidator<SymbolTableBuildingContext, ModuleNameValidatorContext>
{
    protected override ModuleNameValidatorContext CreateInnerContext(
        SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        SymbolTableBuildingContext outerContext, 
        ModuleNameValidatorContext context) => new(context);

    protected override VisitResult PreVisitRoot(RootNode node, ModuleNameValidatorContext context, NodeBase? parent)
    {
        if (node.ModuleName == null)
        {
            var record = PlampExceptionInfo.ModuleMustHaveName();
            context.Exceptions.Add(context.TranslationTable.SetExceptionToNode(node, record));
            return VisitResult.Break;
        }
        
        context.SymTableBuilder.ModuleName = node.ModuleName.ModuleName;
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitFuncName(FuncNameNode node, ModuleNameValidatorContext context, NodeBase? parent)
    {
        if (parent is null || !node.Value.Equals(context.SymTableBuilder.ModuleName)) return VisitResult.SkipChildren;

        var record = PlampExceptionInfo.MemberCannotHaveSameNameAsDeclaringModule();
        SetExceptionToSymbol(parent, record, context);

        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitTypedefName(TypedefNameNode node, ModuleNameValidatorContext context, NodeBase? parent)
    {
        if (parent is null || !node.Value.Equals(context.SymTableBuilder.ModuleName)) return VisitResult.SkipChildren;

        var record = PlampExceptionInfo.MemberCannotHaveSameNameAsDeclaringModule();
        SetExceptionToSymbol(parent, record, context);
        return VisitResult.SkipChildren;
    }
}