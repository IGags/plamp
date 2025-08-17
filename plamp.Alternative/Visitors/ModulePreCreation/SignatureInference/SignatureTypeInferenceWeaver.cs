using System;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
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
        
        var type = new TypeNode(new MemberNode("void"));
        type.SetType(typeof(void));

        if (!context.SymbolTable.TryGetSymbol(node.Name, out var nameSymbol))
        {
            throw new ArgumentException("Symbol is not found, parser error");
        }
            
        context.SymbolTable.AddSymbol(type, nameSymbol.Key, nameSymbol.Value);
        var newDef = new FuncNode(type, node.Name, node.ParameterList, node.Body);
        Replace(node, newDef, context);
        return VisitResult.Continue;
    }

    protected override VisitResult PreVisitType(TypeNode node, SignatureInferenceInnerContext context, NodeBase? parent)
    {
        if (parent is not FuncNode or ParameterNode || node.Symbol != null) return VisitResult.SkipChildren;
        
        var actualType = TypeResolveHelper.ResolveType(node, context.Exceptions, context.SymbolTable, context.FileName);
        if (actualType != null) node.SetType(actualType);
        return VisitResult.SkipChildren;
    }

    protected override SignatureInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(SignatureInferenceInnerContext innerContext, PreCreationContext outerContext) => innerContext;
}