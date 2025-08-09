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
    protected override VisitResult VisitDef(FuncNode node, SignatureInferenceInnerContext context)
    {
        var returnType = node.ReturnType;
        if (returnType == null)
        {
            var type = new TypeNode(new MemberNode("void"));
            type.SetType(typeof(void));
            if(!context.SymbolTable.TryGetSymbol(node.Name, out var nameSymbol)) 
                throw new ArgumentException("Symbol is not found, parser error");
            
            context.SymbolTable.AddSymbol(type, nameSymbol.Key, nameSymbol.Value);
            var newDef = new FuncNode(type, node.Name, node.ParameterList, node.Body);
            Replace(node, newDef, context);
            node = newDef;
        }
        else
        {
            var actualReturnType = TypeResolveHelper.ResolveType(returnType, context.Exceptions, context.SymbolTable, context.FileName);
            if (actualReturnType != null) returnType.SetType(actualReturnType);
        }

        foreach (var parameter in node.ParameterList)
        {
            var parameterType = parameter.Type;
            var actualParameterType = TypeResolveHelper.ResolveType(parameterType, context.Exceptions,
                context.SymbolTable, context.FileName);
            if (actualParameterType == null) continue;
            parameterType.SetType(actualParameterType);
        }

        if (context.Functions.Remove(node.Name.MemberName))
        {
            context.MemberSet.Add(node.Name.MemberName);
        }
        else if (!context.MemberSet.Contains(node.Name.MemberName))
        {
            context.Functions.Add(node.Name.MemberName, node);
        }
        
        return VisitResult.SkipChildren;
    }

    protected override SignatureInferenceInnerContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(SignatureInferenceInnerContext innerContext, PreCreationContext outerContext) => innerContext;
}