using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.CircularDependency;

/*
 * Валидация дженериков.
 * Поскольку язык поддерживает только структурные типы(пока) и с другой стороны дженерики. Мы должны статически проверить,
 * что разработчик не создал дженерик тип, полем которого является он сам. При этом валидация должна затрагивать не только поля этого типа, но и поля типов его полей.
 * Дженерик тип проверяется только по типам-объявлениям его полей. Так как дженерик вложенный сам в себя не может вызвать бесконечную рекурсию.
 * 
 * ListTuple[T1, T2]
 * |---left: T1
 * |---right: ListTuple[T1, int] <- Вызовет ошибку так как тип-объявление поля вызывает бесконечную рекурсию.
 *
 * Ещё один пример.
 *
 * ValueNode[T]
 * |---val: int
 * |---parent Node[T] <- ошибка
 * 
 * Node[T]
 * |---val: ValueNode[T] <- ошибка
 */
/// <inheritdoc/>
public class TypeHasCircularDependencyValidator : BaseValidator<SymbolTableBuildingContext, SymbolTableBuildingContext>
{
    /// <inheritdoc/>
    protected override VisitorGuard Guard => VisitorGuard.TypeDef;

    /// <inheritdoc/>
    protected override SymbolTableBuildingContext CreateInnerContext(SymbolTableBuildingContext context) => context;

    /// <inheritdoc/>
    protected override SymbolTableBuildingContext MapInnerToOuter(
        SymbolTableBuildingContext outerContext,
        SymbolTableBuildingContext innerContext) 
        => innerContext;

    protected override VisitResult PreVisitTypedef(
        TypedefNode node, 
        SymbolTableBuildingContext context, 
        NodeBase? parent)
    {
        if (!context.SymTableBuilder.TryGetInfo(node.Name.Value, out ITypeBuilderInfo? typeInfo)) return VisitResult.SkipChildren;
        var moduleType = context.SymTableBuilder.ListTypes().Cast<ITypeInfo>().ToList();
        
        foreach (var field in typeInfo.FieldBuilders)
        {
            if (!VisitRecursive(field.FieldType, typeInfo, moduleType))
            {
                continue;
            }
            
            var record = PlampExceptionInfo.FieldProduceCircularDependency();
            if (!typeInfo.TryGetDefinition(field, out var defNode))
            {
                throw new InvalidOperationException("Невозможно найти место объявления поля в файле. Ошибка в коде компилятора.");
            }
                
            SetExceptionToSymbol(defNode, record, context);
        }
        
        return VisitResult.SkipChildren;
    }

    private bool VisitRecursive(ITypeInfo info, ITypeInfo originalType, List<ITypeInfo> moduleTypes)
    {
        if (info is { IsGenericType: false, IsArrayType: false } && !moduleTypes.Contains(info)) return false;
        
        if (info.Equals(originalType)) return true;

        if (TypeIsGenericImplWithOpenParams(originalType, info, moduleTypes)) return true;
        if (TypeIsArrayWithOriginalTypeElement(originalType, info, moduleTypes)) return true;
        
        foreach (var fldType in info.Fields.Select(x => x.FieldType))
        {
            if (VisitRecursive(fldType, originalType, moduleTypes)) return true;
        }

        return false;
    }

    /// <summary>
    /// Проверка, что тип является имплементацией объявления дженерика хотя бы с одним открытым параметром.
    /// </summary>
    /// <param name="genericDef">Определение дженерика, который будет базой</param>
    /// <param name="genericImpl">Проверяемый тип</param>
    /// <param name="moduleTypes">Список типов текущем модуле.</param>
    /// <returns>Флаг указывающий на результат проверки</returns>
    private bool TypeIsGenericImplWithOpenParams(ITypeInfo genericDef, ITypeInfo genericImpl, List<ITypeInfo> moduleTypes)
    {
        if (!genericDef.IsGenericTypeDefinition || !genericImpl.IsGenericType) return false;
        
        var implDef = genericImpl.GetGenericTypeDefinition();
        if (genericDef.Equals(implDef)) return true;
        ArgumentNullException.ThrowIfNull(implDef);

        return VisitRecursive(implDef, genericDef, moduleTypes);
    }

    private bool TypeIsArrayWithOriginalTypeElement(ITypeInfo originalType, ITypeInfo arrayType, List<ITypeInfo> moduleTypes)
    {
        if (!arrayType.IsArrayType) return false;
        var elem = arrayType;
        while (elem.IsArrayType)
        {
            elem = elem.ElementType();
            if (elem == null) throw new Exception("Тип элемента массива не может быть пустым, обратитесь к разработчику компилятора.");
        }

        return VisitRecursive(elem, originalType, moduleTypes);
    }
}