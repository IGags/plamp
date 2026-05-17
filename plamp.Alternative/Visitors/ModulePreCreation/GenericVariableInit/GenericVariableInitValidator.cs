using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.GenericVariableInit;

/// <summary>
/// Переменная типа дженерик без явной установки значения не может быть построена, так как для некоторых типов не может быть найдено инициализатора.
/// Или тип может иметь значение null по умолчанию. 
/// </summary>
public class GenericVariableInitValidator : BaseValidator<PreCreationContext, PreCreationContext>
{
    protected override VisitorGuard Guard => VisitorGuard.FuncDefWithBody;

    /// <inheritdoc/>
    protected override PreCreationContext CreateInnerContext(PreCreationContext context) => context;

    /// <inheritdoc/>
    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, PreCreationContext innerContext) 
        => innerContext;

    protected override VisitResult PreVisitVariableDefinition(VariableDefinitionNode node, PreCreationContext context, NodeBase? parent)
    {
        if (parent is not BodyNode) return VisitResult.SkipChildren;
        var info = node.Type?.TypeInfo;
        if (info is null) return VisitResult.SkipChildren;
        if(!info.IsGenericTypeParameter) return VisitResult.SkipChildren;

        var record = PlampExceptionInfo.CannotCreateGenericParameterType();
        SetExceptionToSymbol(node, record, context);
        
        return VisitResult.SkipChildren;
    }
}