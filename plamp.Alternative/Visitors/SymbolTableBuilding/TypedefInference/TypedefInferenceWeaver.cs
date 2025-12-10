using System;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Intrinsics;

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
        if (RuntimeSymbols.SymbolTable.TryGetTypeByName(node.Name.Value, [], out _))
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
                if(!context.TranslationTable.TryGetSymbol(type, out var position)) throw new Exception("Type is not found in source file");
                var typeRef = context.CurrentModuleTable.TryAddType(type.Name.Value, position);
                if(typeRef != null) type.SetTypeInfo(typeRef);
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