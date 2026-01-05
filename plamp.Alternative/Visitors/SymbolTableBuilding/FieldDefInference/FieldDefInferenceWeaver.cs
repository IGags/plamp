using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;

public class FieldDefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, FieldInferenceInnerContext>
{
    protected override FieldInferenceInnerContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        FieldInferenceInnerContext innerContext,
        SymbolTableBuildingContext outerContext) 
        => innerContext;

    protected override VisitResult PreVisitFieldDef(FieldDefNode defNode, FieldInferenceInnerContext context, NodeBase? parent)
    {
        if (parent is TypedefNode parentType && defNode.Name.Value == parentType.Name.Value)
        {
            var nameRecord = PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType();
            SetExceptionToSymbol(defNode.Name, nameRecord, context);
            return VisitResult.SkipChildren;
        }
        
        var fieldType = defNode.FieldType;
        var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(
            fieldType.TypeName.Name, 
            context.Dependencies.Concat([(ISymTable)context.SymTableBuilder]), 
            out var info);
        
        if (record != null)
        {
            SetExceptionToSymbol(fieldType, record, context);
            return VisitResult.SkipChildren;
        }

        for (var i = 0; i < fieldType.ArrayDefinitions.Count; i++)
        {
            info = info!.MakeArrayType();
        }
        
        defNode.FieldType.TypeInfo = info;
        context.Fields.Add(defNode);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitTypedef(TypedefNode node, FieldInferenceInnerContext context, NodeBase? parent)
    {
        var duplicateIndexes = new HashSet<int>();
        
        for (var i = 0;        i < context.Fields.Count; i++)
        for (var j = i + 1; j < context.Fields.Count; j++)
        {
            if (context.Fields[i].Name.Value != context.Fields[j].Name.Value) continue;
            duplicateIndexes.Add(i);
            duplicateIndexes.Add(j);
        }

        if (context.Fields.Count == 0) return VisitResult.Continue;

        var type = context.SymTableBuilder.ListTypes().FirstOrDefault(x => x.Definition == node);
        if (type == null) return VisitResult.Continue;
        
        for (var i = 0; i < context.Fields.Count; i++)
        {
            var fieldName = context.Fields[i].Name.Value;
            if (duplicateIndexes.Contains(i))
            {
                var record = PlampExceptionInfo.DuplicateFieldDefinition(fieldName);
                SetExceptionToSymbol(node, record, context);
                continue;
            }

            var field = context.Fields[i];
            type.AddField(field);
        }
        
        context.Fields.Clear();
        
        return VisitResult.Continue;
    }
}