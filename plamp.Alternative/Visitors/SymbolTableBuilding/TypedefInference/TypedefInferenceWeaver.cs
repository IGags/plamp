using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;

/// <summary>
/// Визитор, который обходит все типы в модуле, валидирует и создаёт их представление в таблице символов, если они валидны.
/// </summary>
public class TypedefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, TypedefInferenceVisitorContext>
{
    protected override TypedefInferenceVisitorContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        SymbolTableBuildingContext outerContext,
        TypedefInferenceVisitorContext innerContext)
        => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, TypedefInferenceVisitorContext context, NodeBase? parent)
    {
        //В случае если тип имеет имя, которое равно имени встроенного типа - будет получена ошибка 
        if (Builtins.SymTable.ContainsSymbol(node.Name.Value))
        {
            var record = PlampExceptionInfo.CannotDefineCoreType();
            SetExceptionToSymbol(node, record, context);
            return VisitResult.SkipChildren;
        }
        
        context.Types.Add(node);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitRoot(RootNode node, TypedefInferenceVisitorContext context, NodeBase? parent)
    {
        /*
         * Все валидные по названию типы группируются по имени, а потом среди них находятся дубликаты.
         * Все типы с дублирующимся наименованием помечаются ошибочными и исключаются из таблицы символов
         */
        var typeGroups = context.Types.GroupBy(x => x.Name.Value);
        foreach (var types in typeGroups)
        {
            var typeList = types.ToList();
            //Валидация дубликатов имён - не ответственность этого вивера.
            if (typeList.Count != 1) continue;
            var type = typeList[0];
            var generics = TryValidateGenericsIfExists(type, context);
            context.SymTableBuilder.DefineType(type, generics);
        }

        return VisitResult.Continue;
    }
    
    /// <summary>
    /// Проверяет наличие дженерик параметров и корректность их объявления в контексте текущего типа.
    /// Под корректностью понимается
    /// 1 - не соответствие имени параметра имени содержащего его типа
    /// 2 - не соответствие имени параметра имени встроенного типа
    /// 3 - уникальность имени параметра в контексте списка дженерик параметров текущего типа
    /// Если всё корректно, то модифицирует объект объявления типа с учётом объявленных дженериков
    /// </summary>
    /// <param name="typeDefinition">Объект, описывающий объявление типа в контексте текущего модуля</param>
    /// <param name="context">Контекст обхода текущего модуля</param>
    private IGenericParameterBuilder[] TryValidateGenericsIfExists(
        TypedefNode typeDefinition, 
        TypedefInferenceVisitorContext context)
    {
        var parameters = typeDefinition.GenericParameters;
        if (parameters.Count == 0) return [];

        var nameGrouping = parameters.GroupBy(x => x.Name.Value);
        
        var definingTypeName = typeDefinition.Name.Value;
        var validGenerics = new List<IGenericParameterBuilder>();
        
        foreach (var group in nameGrouping)
        {
            var groupArray = group.ToArray();
            var parameterName = groupArray[0].Name.Value;
            PlampExceptionRecord? record = null;
            
            if (Builtins.SymTable.FindType(parameterName) != null)
            {
                record = PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember();
            }
            else if (definingTypeName.Equals(parameterName))
            {
                record = PlampExceptionInfo.GenericParameterNameSameAsDefiningType();
            }
            else if (groupArray.Length != 1)
            {
                record = PlampExceptionInfo.DuplicateGenericParameterName();
            }

            if (record == null)
            {
                validGenerics.Add(context.SymTableBuilder.CreateGenericParameter(groupArray[0]));
                continue;
            }
            
            foreach (var parameter in groupArray)
            {
                SetExceptionToSymbol(parameter, record, context);
            }
        }
        
        return validGenerics.ToArray();
    }
}