using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Intrinsics;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;

public class FuncDefInferenceWeaver : BaseWeaver<SymbolTableBuildingContext, FuncDefInferenceContext>
{
    protected override FuncDefInferenceContext CreateInnerContext(SymbolTableBuildingContext context)
        => new(context);

    protected override SymbolTableBuildingContext MapInnerToOuter(
        FuncDefInferenceContext innerContext,
        SymbolTableBuildingContext outerContext)
        => innerContext;

    protected override VisitResult PreVisitParameter(ParameterNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        var typeNode = node.Type;
        return ResolveTypeOrSetError(typeNode.TypeName.Name, typeNode.ArrayDefinitions, typeNode, context);
    }

    protected override VisitResult PreVisitFunction(FuncNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        if (node.ReturnType == null)
        {
            var voidType = RuntimeSymbols.SymbolTable.Void;
            var typ = new TypeNode(new TypeNameNode(voidType.TypeName));
            typ.SetTypeRef(voidType);
            if (!context.TranslationTable.TryGetSymbol(node.FuncName, out var nameSymbol))
            {
                throw new ArgumentException("Symbol is not found, parser error");
            }
        
            context.TranslationTable.AddSymbol(typ, nameSymbol);
            var newDef = new FuncNode(typ, node.FuncName, node.ParameterList, node.Body);
            Replace(node, newDef, context);
            return VisitResult.Continue;
        }

        var returnType = node.ReturnType;
        ResolveTypeOrSetError(returnType.TypeName.Name, returnType.ArrayDefinitions, node.ReturnType!, context);
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
            var ambigulousIndexes = new HashSet<int>();
            
            for (var i = 0       ; i < overloadList.Count; i++)
            for (var j = i + 1; j < overloadList.Count; j++)
            {
                if (SignatureMatch(overloadList[i].ParameterList, overloadList[j].ParameterList))
                {
                    ambigulousIndexes.Add(i);
                    ambigulousIndexes.Add(j);
                }                               
            }

            var record = PlampExceptionInfo.DuplicateFunctionDefinition(funcOverload.Key);
            for (var i = 0; i < overloadList.Count; i++)
            {
                if (ambigulousIndexes.Contains(i)) SetExceptionToSymbol(overloadList[i], record, context);
                else AddFuncToSymbols(overloadList[i], context);
            }
        }

        return VisitResult.Continue;
    }

    private void AddFuncToSymbols(FuncNode node, FuncDefInferenceContext context)
    {
        var args = node.ParameterList;
        ValidateParameters(args, context);
        
        var argTypes = args.Select(x => x.Type.TypedefRef).ToList();
        
        //Если node.ReturnType - значит тип void, иначе тип невозможно определить
        var returnType = node.ReturnType == null ? RuntimeSymbols.SymbolTable.Void : node.ReturnType.TypedefRef;
        if (returnType == null || argTypes.Any(x => x == null)) return;
        
        if (!context.TranslationTable.TryGetSymbol(node, out var position)) throw new Exception("Parser error func def location is not set");
        context.CurrentModuleTable.TryAddFunc(node.FuncName.Value, returnType, argTypes!, position, out _);
    }

    private void ValidateParameters(List<ParameterNode> parameters, FuncDefInferenceContext context)
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

    private bool SignatureMatch(List<ParameterNode> first, List<ParameterNode> second)
    {
        if (first.Count != second.Count) return false;
        for (var i = 0; i < first.Count; i++)
        {
            var firstRef = first[i].Type.TypedefRef;
            var secondRef = second[i].Type.TypedefRef;
            if (firstRef != null && secondRef != null && firstRef.Equals(secondRef)) continue;
            if (firstRef != null && secondRef != null && firstRef.Equals(secondRef)) return false;
            if (first[i].Type.TypeName.Name != second[i].Type.TypeName.Name) return false;
        }

        return true;
    }

    private VisitResult ResolveTypeOrSetError(string typeName, List<ArrayTypeSpecificationNode> arrayDefs, TypeNode typeNode, FuncDefInferenceContext context)
    {
        var record = TypeResolveHelper.FindTypeByName(typeName, arrayDefs, context.Dependencies, out var type);
        if (record != null)
        {
            SetExceptionToSymbol(typeNode, record, context);
            return VisitResult.SkipChildren;
        }
        
        typeNode.SetTypeRef(type!);
        return VisitResult.SkipChildren;
    }
}