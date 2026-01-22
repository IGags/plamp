using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;

public class TypedefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, TypedefInferenceVisitorContext>
{
    protected override TypedefInferenceVisitorContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        TypedefInferenceVisitorContext innerContext,
        SymbolTableBuildingContext outerContext)
        => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, TypedefInferenceVisitorContext context, NodeBase? parent)
    {
        if (Builtins.SymTable.FindType(node.Name.Value) != null)
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
        var typeGroups = context.Types.GroupBy(x => x.Name.Value);
        foreach (var types in typeGroups)
        {
            var typeList = types.ToList();
            if (typeList.Count == 1)
            {
                var type = typeList[0];
                _ = context.SymTableBuilder.DefineType(type);
                continue;
            }

            var record = PlampExceptionInfo.DuplicateTypeDefinition(typeList[0].Name.Value);
            foreach (var type in typeList)
            {
                SetExceptionToSymbol(type, record, context);
            }
        }

        return VisitResult.Continue;
    }
}