using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;

/// <summary>
/// Посетитель, обходящий все поля всех валидных типов внутри модуля и добавляющий их в таблицу символов. 
/// </summary>
public class FieldDefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, FieldInferenceInnerContext>
{
    protected override VisitorGuard Guard => VisitorGuard.TypeDef;

    protected override FieldInferenceInnerContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        SymbolTableBuildingContext outerContext,
        FieldInferenceInnerContext innerContext) 
        => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, FieldInferenceInnerContext context, NodeBase? parent)
    {
        if (!context.SymTableBuilder.TryGetInfo(node.Name.Value, out ITypeBuilderInfo? typedef)) return VisitResult.Continue;
        context.TypeGenericList = typedef.GetGenericParameters();
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitTypedef(TypedefNode node, FieldInferenceInnerContext context, NodeBase? parent)
    {
        var fieldGrouping = node.Fields.GroupBy(x => x.Name.Value);
        foreach (var nameGroup in fieldGrouping)
        {
            var groupArray = nameGroup.ToArray();
            
            if(!ValidateFieldNameGroup(groupArray, node, context)) continue;
            
            if(!context.SymTableBuilder.TryGetInfo(node.Name.Value, out ITypeBuilderInfo? type)) continue;
            type.AddField(groupArray[0]);
        }
        
        context.TypeGenericList = null;
        return VisitResult.Continue;
    }

    /// <summary>
    /// Проверка валидности объявления полей сгруппированных по имени.
    /// </summary>
    /// <param name="fieldNameGroup">Группа полей в объявлении типа с одинаковым именем</param>
    /// <param name="definingType">Тип, в котором эти поля объявлены</param>
    /// <param name="context">Контекст обхода</param>
    /// <returns>Флаг, в зависимости от значения которого следует или не следует добавлять данное поле в таблицу символов.</returns>
    private bool ValidateFieldNameGroup(FieldDefNode[] fieldNameGroup, TypedefNode definingType, FieldInferenceInnerContext context)
    {
        //Если ничего не передали, то всегда возвращаем ложь
        if (fieldNameGroup.Length == 0) return false;
        PlampExceptionRecord? record = null;

        //Логика валидации группы полей.
        //1 - имя поля не должно совпадать с именем типа, в котором оно объявлено
        //2 - имя поля не должно совпадать с именами встроенных типов
        //3 - внутри одного типа не может быть 2 поля с одинаковым именем.
        if (fieldNameGroup[0].Name.Value.Equals(definingType.Name.Value))
        {
            record = PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType();
        }
        else if (Builtins.SymTable.FindType(fieldNameGroup[0].Name.Value) != null)
        {
            record = PlampExceptionInfo.FieldHasSameNameAsBuiltinMember();
        }
        else if (fieldNameGroup.Length != 1)
        {
            record = PlampExceptionInfo.DuplicateFieldDefinition(fieldNameGroup[0].Name.Value);
        }

        // Добавляем все ошибки
        if (record != null)
        {
            foreach (var fld in fieldNameGroup)
            {
                SetExceptionToSymbol(fld, record, context);
            }

            return false;
        }
        
        //Следует добавлять ли поле в таблицу символов также зависит от того, есть ли у этого поля тип или нет.
        var field = fieldNameGroup[0];
        return field.FieldType.TypeInfo != null;
    }
    
    protected override VisitResult PostVisitType(TypeNode node, FieldInferenceInnerContext context, NodeBase? parent)
    {
        // Прямой признак того, что мы не находимся внутри типа, такое мы не обходим.
        if (context.TypeGenericList == null) return VisitResult.SkipChildren;
        
        //Эта штука обходит тип рекурсивно в глубину, поэтому дженерики уже имеют тип, если он известен.
        var genericArgs = node.GenericParameters
            .Select(x => x.TypeInfo)
            .Where(x => x != null)
            .Cast<ITypeInfo>()
            .ToList();
        
        if (genericArgs.Count != node.GenericParameters.Count) return VisitResult.SkipChildren;

        //Попытка записать в тип дженерик параметр объявляющего типа.
        if (genericArgs.Count == 0)
        {
            var typeInfo = context.TypeGenericList.FirstOrDefault(x => x.Name == node.TypeName.Name);
            if (typeInfo != null)
            {
                node.TypeInfo = MakeArrayTypeInfoFromElem(typeInfo, node);
                return VisitResult.Continue;
            }
        }

        //Чёт ищем
        var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(
            node,
            context.Dependencies.Concat([(ISymTable)context.SymTableBuilder]),
            out var info);

        if (record != null)
        {
            SetExceptionToSymbol(node, record, context);
            node.TypeInfo = null;
            return VisitResult.SkipChildren;
        }

        if (info!.IsGenericTypeDefinition)
        {
            if (genericArgs.Count != info.GetGenericParameters().Count)
            {
                return VisitResult.SkipChildren;
            }
            
            info = info.MakeGenericType(genericArgs);
        }

        node.TypeInfo = MakeArrayTypeInfoFromElem(info!, node);
        return VisitResult.Continue;

        ITypeInfo MakeArrayTypeInfoFromElem(ITypeInfo elemType, TypeNode typeNode)
        {
            for (var i = 0; i < typeNode.ArrayDefinitions.Count; i++)
            {
                elemType = elemType.MakeArrayType();
            }
            return elemType;
        }
    }
}