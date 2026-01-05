using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.CircularDependency;

public class TypeHasCircularDependencyValidator : BaseValidator<SymbolTableBuildingContext, SymbolTableBuildingContext>
{
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
        var typeInfo = context.SymTableBuilder.ListTypes().FirstOrDefault(x => x.Definition == node);
        if (typeInfo == null) return VisitResult.SkipChildren;
        var moduleType = context.SymTableBuilder.ListTypes().Cast<ITypeInfo>().ToList();
        
        foreach (var field in typeInfo.FieldBuilders)
        {
            if (VisitRecursive(field.FieldType, typeInfo, moduleType))
            {
                var record = PlampExceptionInfo.FieldProduceCircularDependency();
                SetExceptionToSymbol(field.Definition, record, context);
            }
        }
        
        return VisitResult.SkipChildren;
    }

    private bool VisitRecursive(ITypeInfo info, ITypeInfo originalType, List<ITypeInfo> moduleTypes)
    {
        if (!moduleTypes.Contains(info)) return false;
        if (info == originalType) return true;
        foreach (var fldType in info.Fields.Select(x => x.FieldType))
        {
            if (VisitRecursive(fldType, originalType, moduleTypes)) return true;
        }

        return false;
    }
}