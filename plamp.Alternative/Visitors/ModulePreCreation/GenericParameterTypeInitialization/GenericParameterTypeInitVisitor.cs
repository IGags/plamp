using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.GenericParameterTypeInitialization;

/// <summary>
/// Проверяет, что нельзя создать объект типа дженерик параметра из-за того, что некоторые реализации этого типа могут быть не null.
/// </summary>
public class GenericParameterTypeInitVisitor : BaseValidator<PreCreationContext, PreCreationContext>
{
    protected override VisitorGuard Guard => VisitorGuard.FuncDefWithBody;

    /// <inheritdoc/>
    protected override PreCreationContext CreateInnerContext(PreCreationContext context) => context;

    /// <inheritdoc/>
    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, PreCreationContext innerContext)
        => innerContext;

    /// <inheritdoc/>
    protected override VisitResult PreVisitInitType(InitTypeNode node, PreCreationContext context, NodeBase? parent)
    {
        if (node.Type.TypeInfo is not { IsGenericTypeParameter: true }) return VisitResult.Continue;

        var record = PlampExceptionInfo.CannotCreateGenericParameterType();
        SetExceptionToSymbol(node, record, context);
        return VisitResult.SkipChildren;
    }
}
