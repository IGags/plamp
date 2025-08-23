using System;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;

/// <summary>
/// Определяет только типы аргументов
/// </summary>
public class SignatureTypeInferenceWeaver : BaseWeaver<PreCreationContext, SignatureInferenceInnerContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, SignatureInferenceInnerContext context, NodeBase? parent)
    {
        if (node.ReturnType != null) return VisitResult.Continue;
        if (context.Functions.Remove(node.FuncName.Value)) return VisitResult.SkipChildren;
        var type = new TypeNode(new TypeNameNode("void"));
        type.SetType(typeof(void));

        if (!context.SymbolTable.TryGetSymbol(node.FuncName, out var nameSymbol))
        {
            throw new ArgumentException("Symbol is not found, parser error");
        }
            
        context.SymbolTable.AddSymbol(type, nameSymbol.Key, nameSymbol.Value);
        var newDef = new FuncNode(type, node.FuncName, node.ParameterList, node.Body);
        context.Functions.Add(newDef.FuncName.Value, newDef);
        Replace(node, newDef, context);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitFunction(FuncNode node, SignatureInferenceInnerContext context, NodeBase? parent)
    {
        if (node.ReturnType == null) return VisitResult.Continue;
        if (context.Functions.Remove(node.FuncName.Value)) return VisitResult.Continue;
        context.Functions.Add(node.FuncName.Value, node);
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitType(TypeNode node, SignatureInferenceInnerContext context, NodeBase? parent)
    {
        if(parent is not FuncNode and not ParameterNode || node.Symbol != null) return VisitResult.SkipChildren;
        var actualType = TypeResolveHelper.ResolveType(node, context.Exceptions, context.SymbolTable, context.FileName);
        if (actualType != null) node.SetType(actualType);
        return VisitResult.SkipChildren;
    }

    protected override SignatureInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(SignatureInferenceInnerContext innerContext, PreCreationContext outerContext) => innerContext;
}