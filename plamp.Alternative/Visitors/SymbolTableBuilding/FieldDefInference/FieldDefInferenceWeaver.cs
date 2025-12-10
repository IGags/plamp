using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;

public class FieldDefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, FieldInferenceInnerContext>
{
    protected override FieldInferenceInnerContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        FieldInferenceInnerContext innerContext,
        SymbolTableBuildingContext outerContext) 
        => innerContext;

    protected override VisitResult PreVisitFieldDefNode(FieldDefNode defNode, FieldInferenceInnerContext context, NodeBase? parent)
    {
        if (parent is TypedefNode parentType && defNode.Name.Value == parentType.Name.Value)
        {
            var nameRecord = PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType();
            SetExceptionToSymbol(defNode.Name, nameRecord, context);
            return VisitResult.SkipChildren;
        }
        
        var fieldType = defNode.FieldType;
        
        var record = TypeResolveHelper.FindTypeByName(
            fieldType.TypeName.Name, 
            fieldType.ArrayDefinitions,  
            context.Dependencies, 
            out var typeRef);
        
        if (record != null)
        {
            SetExceptionToSymbol(defNode.FieldType, record, context);
            return VisitResult.SkipChildren;
        }
        
        defNode.FieldType.SetTypeRef(typeRef!);
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

        for (var i = 0; i < context.Fields.Count; i++)
        {
            var fieldName = context.Fields[i].Name.Value;
            if (duplicateIndexes.Contains(i))
            {
                var record = PlampExceptionInfo.DuplicateFieldDefinition(fieldName);
                SetExceptionToSymbol(node, record, context);
            }
            else
            {
                var parentType = node.TypeInfo;
                var fieldInfo = context.Fields[i].FieldType.TypedefRef;
                if(fieldInfo == null) continue;
                parentType?.DefineField(fieldName, fieldInfo);
            }
        }
        
        context.Fields.Clear();
        
        return VisitResult.Continue;
    }
}