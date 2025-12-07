using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Intrinsics;

namespace plamp.Alternative.Visitors.ModulePreCreation.FuncDefInference;

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
            var voidType = RuntimeSymbols.SymbolTable.MakeVoid();
            var typ = new TypeNode(new TypeNameNode(voidType.TypeName));
            typ.SetTypeRef(voidType);
            if (!context.TranslationTable.TryGetSymbol(node.FuncName, out var nameSymbol))
            {
                throw new ArgumentException("Symbol is not found, parser error");
            }
        
            context.TranslationTable.AddSymbol(typ, nameSymbol);
            return VisitResult.Continue;
        }

        var typeNode = node.ReturnType;
        ResolveTypeOrSetError(typeNode.TypeName.Name, typeNode.ArrayDefinitions, node.ReturnType!, context);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitFunction(FuncNode node, FuncDefInferenceContext context, NodeBase? parent)
    {
        if (node.ReturnType?.TypedefRef == null) return VisitResult.Continue;
        
        context.TranslationTable.TryGetSymbol(node, out var funcDefPos);
        if (context.Duplicates.ContainsKey(node.FuncName.Value))
        {
            context.Duplicates[node.FuncName.Value].Add(funcDefPos);
            return VisitResult.SkipChildren;
        }

        var parameterDuplicates = new Dictionary<string, List<ParameterNode>>();
        var parameters = new Dictionary<string, ParameterNode>();
        foreach (var parameterNode in node.ParameterList)
        {
            if (parameterDuplicates.ContainsKey(parameterNode.Name.Value))
            {
                parameterDuplicates[parameterNode.Name.Value].Add(parameterNode);
                continue;
            }

            if (parameters.Remove(parameterNode.Name.Value, out var parameter))
            {
                parameterDuplicates.Add(parameterNode.Name.Value, [parameter, parameterNode]);
                continue;
            }

            parameters[parameterNode.Name.Value] = parameterNode;
        }

        var duplicateParamRecord = PlampExceptionInfo.DuplicateParameterName();
        foreach (var duplicate in parameterDuplicates)
        {
            foreach (var duplicateParameter in duplicate.Value)
            {
                SetExceptionToSymbol(duplicateParameter, duplicateParamRecord, context);
            }
        }
        
        var argumentTypes = parameters.Values
            .Where(x => x.Type.TypedefRef != null)
            .Select(x => x.Type.TypedefRef).ToList();

        var added = context.CurrentModuleTable.TryAddFunc(
            node.FuncName.Value, 
            node.ReturnType.TypedefRef, 
            argumentTypes!,
            funcDefPos,
            out var fnRef);

        if (added)
        {
            return VisitResult.Continue;
        }
        
        context.Duplicates[node.FuncName.Value] = [fnRef.GetDefinitionInfo().DefinitionPosition, funcDefPos];
        return VisitResult.SkipChildren;
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