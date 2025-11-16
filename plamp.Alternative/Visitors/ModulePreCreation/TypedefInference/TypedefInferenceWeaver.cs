using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypedefInference;

public class TypedefInferenceWeaver : BaseWeaver<TypedefInferenceVisitorContext, TypedefInferenceVisitorContext>
{
    protected override TypedefInferenceVisitorContext CreateInnerContext(
        TypedefInferenceVisitorContext context) 
        => context;

    protected override TypedefInferenceVisitorContext MapInnerToOuter(
        TypedefInferenceVisitorContext innerContext,
        TypedefInferenceVisitorContext outerContext)
        => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, TypedefInferenceVisitorContext context, NodeBase? parent)
    {
        if (context.Types.Remove(node.Name.Value)) return VisitResult.SkipChildren;
        var fields = node.Fields;

        var fieldDescList = new List<KeyValuePair<string, FieldNode>>();
        foreach (var field in fields)
        {
            foreach (var name in field.Names)
            {
                fieldDescList.Add(new (name.Value, field));
            }
        }

        return VisitResult.SkipChildren;
    }
}