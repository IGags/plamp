using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.Visitors.ModulePreCreation.FieldDefInference;

public class FieldDefInferenceWeaver : BaseWeaver<PreCreationContext, FieldInferenceInnerContext>
{
    protected override FieldInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(
        FieldInferenceInnerContext innerContext,
        PreCreationContext outerContext) 
        => innerContext;

    protected override VisitResult PreVisitFieldNode(FieldNode node, FieldInferenceInnerContext context, NodeBase? parent)
    {
        var fieldType = node.FieldType;
        
        var record = TypeResolveHelper.FindTypeByName(fieldType.TypeName.Name, fieldType.ArrayDefinitions,  [context.SymbolTable], out var typeRef);
        if (record != null)
        {
            SetExceptionToSymbol(node.FieldType, record, context);
            return VisitResult.SkipChildren;
        }
        
        node.FieldType.SetTypeRef(typeRef!);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitTypedef(TypedefNode node, FieldInferenceInnerContext context, NodeBase? parent)
    {
        var fields = new Dictionary<string, KeyValuePair<FieldNameNode, TypeNode>>();
        foreach (var fieldDef in node.Fields)
        {
            foreach (var fieldName in fieldDef.Names)
            {
                if (context.Duplicates.ContainsKey(fieldName.Value))
                {
                    context.Duplicates[fieldName.Value].Add(fieldName);
                    continue;
                }

                if (fields.Remove(fieldName.Value, out var duplicate))
                {
                    context.Duplicates[fieldName.Value] = [duplicate.Key, fieldName];
                    continue;
                }

                fields[fieldName.Value] = new (fieldName, fieldDef.FieldType);
            }
        }

        foreach (var duplicateList in context.Duplicates)
        {
            var record = PlampExceptionInfo.DuplicateFieldDefinition(duplicateList.Key);
            foreach (var fieldName in duplicateList.Value)
            {
                SetExceptionToSymbol(fieldName, record, context);
            }
        }

        var definingType = node.TypeInfo;
        if (definingType == null) return VisitResult.SkipChildren;
        
        foreach (var field in fields)
        {
            var fldType = field.Value.Value.TypedefRef;
            if (fldType == null) continue;
            definingType.DefineField(field.Key, fldType);
        }

        return VisitResult.Continue;
    }
}