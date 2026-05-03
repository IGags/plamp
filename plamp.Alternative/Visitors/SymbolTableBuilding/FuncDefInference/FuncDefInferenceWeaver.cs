using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.ComplexTypes;
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

    protected override VisitResult PostVisitType(TypeNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        ITypeInfo? type = null;
        
        //Если тип по имени совпадает с дженерик параметром.
        if (node.GenericParameters.Count == 0)
        {
            var typeName = node.TypeName.Name;
            var genericParam = context.CurrentFuncGenerics.FirstOrDefault(x => x.Name.Equals(typeName));
            if (genericParam != null)
            {
                type = genericParam;
            }
        }
        
        var generics = node.GenericParameters.Select(x => x.TypeInfo).ToList();
        if (generics.Any(x => x == null)) return VisitResult.SkipChildren;

        if (type == null)
        {
            var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(node, context.Dependencies.Concat([(ISymTable)context.SymTableBuilder]), out type);
            if (record != null)
            {
                SetExceptionToSymbol(node, record, context);
                return VisitResult.SkipChildren;
            }
        }

        if (node.GenericParameters.Count > 0)
        {
            var genericArgList = generics.OfType<ITypeInfo>().ToList();
            type = type!.MakeGenericType(genericArgList);
        }
        
        for (var i = 0; i < node.ArrayDefinitions.Count; i++)
        {
            type = type!.MakeArrayType();
        }
        
        node.TypeInfo = type;
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitGenericDefinition(GenericDefinitionNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        if (parent is FuncNode parentFn && node.Name.Value.Equals(parentFn.FuncName.Value))
        {
            var record = PlampExceptionInfo.GenericParamSameNameAsDefiningFunction();
            SetExceptionToSymbol(node, record, context);
            return VisitResult.SkipChildren;
        }

        if (Builtins.SymTable.ContainsSymbol(node.Name.Value))
        {
            var record = PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember();
            SetExceptionToSymbol(node, record, context);
            return VisitResult.SkipChildren;
        }
        
        var genericParam = context.SymTableBuilder.CreateGenericParameter(node);
        context.CurrentFuncGenerics.Add(genericParam);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitFunction(FuncNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        context.CurrentFuncGenerics.Clear();
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitFunction(FuncNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        if (Builtins.SymTable.ContainsSymbol(node.FuncName.Value))
        {
            var record = PlampExceptionInfo.CannotDefineCoreFunction();
            SetExceptionToSymbol(node, record, context);
        }
        
        if (context.DuplicateNames.Contains(node.FuncName.Value)) return VisitResult.SkipChildren;
        AddFuncToSymbols(node, context);
        return VisitResult.SkipChildren;
    }

    protected override VisitResult PreVisitRoot(RootNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        var nameGrouping = node.Functions.Select(x => x.FuncName.Value)
            .GroupBy(x => x)
            .Where(x => x.Count() > 1);

        context.DuplicateNames = nameGrouping.Select(x => x.Key).ToHashSet();
        
        return VisitResult.Continue;
    }

    private void AddFuncToSymbols(FuncNode node, FuncDefInferenceContext context)
    {
        var args = node.ParameterList;
        if(!ValidParameters(args, context)) return;
        if(!ValidGenerics(node, context)) return;
        
        var argTypes = args.Select(x => new KeyValuePair<string, ITypeInfo?>(x.Name.Value, x.Type.TypeInfo)).ToList();
        
        var returnType = node.ReturnType.TypeInfo;
        if (returnType == null || argTypes.Any(x => x.Value == null)) return;
        _ = context.SymTableBuilder.DefineFunc(node, context.CurrentFuncGenerics.ToArray());
    }

    private bool ValidGenerics(FuncNode node, FuncDefInferenceContext context)
    {
        var genericNameGrouping = node.GenericArgTypes.GroupBy(x => x.Name.Value);
        var record = PlampExceptionInfo.DuplicateGenericParameterName();

        var valid = true;
        foreach (var group in genericNameGrouping)
        {
            if(group.Count() == 1) continue;
            foreach (var genericDef in group)
            {
                SetExceptionToSymbol(genericDef, record, context);
                valid = false;
            }
        }

        return valid;
    }

    private bool ValidParameters(IReadOnlyList<ParameterNode> parameters, FuncDefInferenceContext context)
    {
        var dupArgsIndexes = new HashSet<int>();
        for (var i = 0; i < parameters.Count; i++)
        for (var j = i + 1; j < parameters.Count; j++)
        {
            if (parameters[i].Name.Value != parameters[j].Name.Value) continue;
            dupArgsIndexes.Add(i);
            dupArgsIndexes.Add(j);
        }

        if(dupArgsIndexes.Count == 0) return true;
        var record = PlampExceptionInfo.DuplicateParameterName();
        foreach (var ix in dupArgsIndexes)
        {
            SetExceptionToSymbol(parameters[ix], record, context);
        }

        return false;
    }
}