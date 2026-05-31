using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.GenericArrayInitialization;

/// <summary>
/// Служит для предотвращения ситуаций инициализации непустого массива от дженерик параметра.
/// Это сделано из-за того, что в языке любой тип, кроме явного указания не может быть null.
/// А инициализация массива от дженерик параметра может привести к ошибкам, так как дженерик параметр может иметь тип, допускающий null, или тип без инициализатора со значением по умолчанию null.
/// </summary>
public class GenericArrayInitVisitor : BaseValidator<PreCreationContext, PreCreationContext>
{
    /// <inheritdoc/>
    protected override VisitorGuard Guard => VisitorGuard.FuncDefWithBody;

    /// <inheritdoc/>
    protected override PreCreationContext CreateInnerContext(PreCreationContext context) => context;

    /// <inheritdoc/>
    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, PreCreationContext innerContext) 
        => innerContext;

    /// <inheritdoc/>
    protected override VisitResult PreVisitInitArray(InitArrayNode node, PreCreationContext context, NodeBase? parent)
    {
        var info = node.ArrayItemType.TypeInfo;
        if (info is null) return VisitResult.SkipChildren;
        if(!info.IsGenericTypeParameter) return VisitResult.SkipChildren;
        
        if (node.LengthDefinition is LiteralNode lit)
        {
            if(!NeedToSetException(lit)) return VisitResult.SkipChildren;
        }
        if (node.LengthDefinition is CastNode { Inner: LiteralNode literal })
        {
            if(!NeedToSetException(literal)) return VisitResult.SkipChildren;
        }
        
        var record = PlampExceptionInfo.CannotCreateNonEmptyArrayOfGenericParameter();
        SetExceptionToSymbol(node, record, context);
        return VisitResult.SkipChildren;
    }

    /// <summary>
    /// Метод говорящий, надо ли устанавливать ошибку для такого литерала.
    /// Ошибка ставится для любого литерала,
    /// который может быть приведён в Int без потери информации или ошибок (не Long или Float),
    /// который при этом имеет ненулевое значение
    /// </summary>
    /// <param name="literal">Узел, который надо проверить</param>
    /// <returns>Флаг необходимости установки ошибки</returns>
    private bool NeedToSetException(LiteralNode literal)
    {
        if (!SymbolSearchUtility.IsNumeric(literal.Type)) return false;
        var value = literal.Value;
        if (value == null)             return false;
        if (value is byte and not 0)   return true;
        if (value is sbyte and not 0)  return true;
        if (value is short and not 0)  return true;
        if (value is ushort and not 0) return true;
        if (value is int and not 0)    return true;
        if (value is uint and not 0)   return true;
        return false;
    }
}