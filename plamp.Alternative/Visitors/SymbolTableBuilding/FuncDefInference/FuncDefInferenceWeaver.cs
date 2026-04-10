using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;

public class FuncDefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, FuncDefInferenceContext>
{
    protected override VisitorGuard Guard => VisitorGuard.FuncDef;

    protected override FuncDefInferenceContext CreateInnerContext(SymbolTableBuildingContext context)
        => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        FuncDefInferenceContext innerContext,
        SymbolTableBuildingContext outerContext)
        => innerContext;

    protected override VisitResult PreVisitParameter(ParameterNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        var typeNode = node.Type;
        return ResolveTypeOrSetError(typeNode, context);
    }

    protected override VisitResult PreVisitFunction(FuncNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        ResolveTypeOrSetError(node.ReturnType, context);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitFunction(FuncNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        context.ModuleFunctions.Add(node);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitRoot(RootNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        var overloadGrouping = context.ModuleFunctions.GroupBy(x => x.FuncName.Value, y => y);
        foreach (var funcOverload in overloadGrouping)
        {
            var overloadList = funcOverload.ToList();
            var ambiguousIndexes = new HashSet<int>();
            
            for (var i = 0       ; i < overloadList.Count; i++)
            for (var j = i + 1; j < overloadList.Count; j++)
            {
                if (SignatureMatch(overloadList[i].ParameterList, overloadList[j].ParameterList))
                {
                    ambiguousIndexes.Add(i);
                    ambiguousIndexes.Add(j);
                }                               
            }

            var record = PlampExceptionInfo.DuplicateFunctionDefinition(funcOverload.Key);
            for (var i = 0; i < overloadList.Count; i++)
            {
                if (ambiguousIndexes.Contains(i)) SetExceptionToSymbol(overloadList[i], record, context);
                else AddFuncToSymbols(overloadList[i], context);
            }
        }

        return VisitResult.Continue;
    }

    private void AddFuncToSymbols(FuncNode node, FuncDefInferenceContext context)
    {
        var args = node.ParameterList;
        ValidateParameters(args, context);
        
        var argTypes = args.Select(x => new KeyValuePair<string, ITypeInfo?>(x.Name.Value, x.Type.TypeInfo)).ToList();
        
        var returnType = node.ReturnType.TypeInfo;
        if (returnType == null || argTypes.Any(x => x.Value == null)) return;
        _ = context.SymTableBuilder.DefineFunc(node);
    }

    private void ValidateParameters(IReadOnlyList<ParameterNode> parameters, FuncDefInferenceContext context)
    {
        var dupArgsIndexes = new HashSet<int>();
        for (var i = 0; i < parameters.Count; i++)
        for (var j = i + 1; j < parameters.Count; j++)
        {
            if (parameters[i].Name.Value != parameters[j].Name.Value) continue;
            dupArgsIndexes.Add(i);
            dupArgsIndexes.Add(j);
        }

        if(dupArgsIndexes.Count == 0) return;
        var record = PlampExceptionInfo.DuplicateParameterName();
        foreach (var ix in dupArgsIndexes)
        {
            SetExceptionToSymbol(parameters[ix], record, context);
        }
    }

    private bool SignatureMatch(IReadOnlyList<ParameterNode> first, IReadOnlyList<ParameterNode> second)
    {
        if (first.Count != second.Count) return false;
        for (var i = 0; i < first.Count; i++)
        {
            var firstRef = first[i].Type.TypeInfo;
            var secondRef = second[i].Type.TypeInfo;
            if (firstRef != null && secondRef != null && firstRef.Equals(secondRef)) continue;
            if (firstRef != null && secondRef != null && firstRef.Equals(secondRef)) return false;
            if (first[i].Type.TypeName.Name != second[i].Type.TypeName.Name) return false;
        }

        return true;
    }

    private VisitResult ResolveTypeOrSetError(TypeNode typeNode, FuncDefInferenceContext context)
    {
        var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(typeNode, context.Dependencies.Concat([(ISymTable)context.SymTableBuilder]), out var type);
        if (record != null)
        {
            SetExceptionToSymbol(typeNode, record, context);
            return VisitResult.SkipChildren;
        }

        for (var i = 0; i < typeNode.ArrayDefinitions.Count; i++)
        {
            type = type!.MakeArrayType();
        }
        
        typeNode.TypeInfo = type;
        return VisitResult.SkipChildren;
    }
}