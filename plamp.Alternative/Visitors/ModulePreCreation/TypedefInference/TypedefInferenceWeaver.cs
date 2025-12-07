using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Intrinsics;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypedefInference;

public class TypedefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, TypedefInferenceVisitorContext>
{
    protected override TypedefInferenceVisitorContext CreateInnerContext(SymbolTableBuildingContext context) => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        TypedefInferenceVisitorContext innerContext,
        SymbolTableBuildingContext outerContext)
        => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, TypedefInferenceVisitorContext context, NodeBase? parent)
    {
        context.TranslationTable.TryGetSymbol(node, out var typedefPos);
        
        if (context.Duplicates.ContainsKey(node.Name.Value))
        {
            context.Duplicates[node.Name.Value].Add(typedefPos);
            return VisitResult.SkipChildren;
        }
        
        if (context.CurrentModuleTable.TryGetTypeByName(node.Name.Value, [], out var info))
        {
            context.Duplicates[node.Name.Value] = [info.GetDefinitionInfo().DefinitionPosition, typedefPos];
            return VisitResult.SkipChildren;
        }
        
        if (RuntimeSymbols.SymbolTable.TryGetTypeByName(node.Name.Value, [], out _))
        {
            var record = PlampExceptionInfo.CannotDefineCoreType();
            SetExceptionToSymbol(node, record, context);
            return VisitResult.SkipChildren;
        }
        
        var typeRef = context.CurrentModuleTable.TryAddType(node.Name.Value, typedefPos);
        //Возможно в случае если было 2 объявления, но проверка выше должна такое нивелировать.
        if(typeRef != null) node.SetTypeInfo(typeRef);
        
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitRoot(RootNode node, TypedefInferenceVisitorContext context, NodeBase? parent)
    {
        foreach (var duplicate in context.Duplicates)
        {
            var record = PlampExceptionInfo.DuplicateTypeDefinition(duplicate.Key);
            foreach (var position in duplicate.Value)
            {
                context.Exceptions.Add(new PlampException(record, position));
            }
        }

        return VisitResult.Continue;
    }
}