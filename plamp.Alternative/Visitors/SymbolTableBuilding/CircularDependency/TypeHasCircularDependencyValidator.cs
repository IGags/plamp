using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.CircularDependency;

/*
 * Валидация дженериков.
 * Поскольку язык поддерживает только структурные типы(пока) и с другой стороны дженерики. Мы должны статически проверить,
 * что разработчик не создал дженерик тип, полем которого является он сам. При этом валидация должна затрагивать не только поля этого типа, но и поля типов его полей.
 * Не валидный дженерик тип для нас это такой тип, в полях которого или в полях типов полей (и тд...) содержится он сам же хотя бы с одним открытым дженерик параметром.
 * Например:
 * 
 * ListTuple[T1, T2]
 * |---left: T1
 * |---right: ListTuple[T1, int] <- Вызовет ошибку так как 1 параметр до сих пор открыт.
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
public class TypeHasCircularDependencyValidator : BaseValidator<SymbolTableBuildingContext, SymbolTableBuildingContext>
{
    protected override VisitorGuard Guard => VisitorGuard.TypeDef;

    protected override SymbolTableBuildingContext CreateInnerContext(SymbolTableBuildingContext context) => context;

    protected override SymbolTableBuildingContext MapInnerToOuter(
        SymbolTableBuildingContext outerContext,
        SymbolTableBuildingContext innerContext) 
        => innerContext;

    protected override VisitResult PreVisitTypedef(
        TypedefNode node, 
        SymbolTableBuildingContext context, 
        NodeBase? parent)
    {
        if (!context.SymTableBuilder.TryGetInfo(node, out var typeInfo)) return VisitResult.SkipChildren;
        var moduleType = context.SymTableBuilder.ListTypes().Cast<ITypeInfo>().ToList();
        
        foreach (var field in typeInfo.FieldBuilders)
        {
            if (!VisitRecursive(field.FieldType, typeInfo, moduleType))
            {
                continue;
            }
            
            var record = PlampExceptionInfo.FieldProduceCircularDependency();
            if (!context.SymTableBuilder.TryGetDefinition(field, out var defNode))
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

        if (TypeIsGenericImplWithOpenParams(originalType, info)) return true;
        if (TypeIsArrayWithOriginalTypeElement(originalType, info)) return true;
        
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
    /// <returns>Флаг указывающий на результат проверки</returns>
    private bool TypeIsGenericImplWithOpenParams(ITypeInfo genericDef, ITypeInfo genericImpl)
    {
        if (!genericDef.IsGenericTypeDefinition || !genericImpl.IsGenericType) return false;
        
        var implDef = genericImpl.GetGenericTypeDefinition();
        if (!genericDef.Equals(implDef)) return false;
        
        var implArgs = genericImpl.GetGenericArguments();
        return implArgs.Any(arg => arg.IsGenericTypeParameter);
    }

    private bool TypeIsArrayWithOriginalTypeElement(ITypeInfo originalType, ITypeInfo arrayType)
    {
        if (!arrayType.IsArrayType) return false;
        var elem = arrayType;
        while (elem.IsArrayType)
        {
            elem = elem.ElementType();
            if (elem == null) throw new Exception("Тип элемента массива не может быть пустым, обратитесь к разработчику компилятора.");
        }

        return originalType.Equals(elem);
    }
}