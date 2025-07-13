using System;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;

/// <summary>
/// Определяет только типы аргументов
/// </summary>
public class SignatureTypeInferenceValidator : BaseValidator<PreCreationContext, PreCreationContext>
{
    protected override VisitResult VisitDef(DefNode node, PreCreationContext context)
    {
        var returnType = node.ReturnType;
        if (returnType == null)
        {
            var type = new TypeNode(new MemberNode("void"));
            type.SetType(typeof(void));
            if(!context.SymbolTable.TryGetSymbol(node.Name, out var nameSymbol)) 
                throw new ArgumentException("Symbol is not found, parser error");
            
            context.SymbolTable.AddSymbol(type, nameSymbol.Key, nameSymbol.Value);
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
        
        context.Functions.Add(node.Name.MemberName, node);
        return VisitResult.SkipChildren;
    }

    protected override PreCreationContext CreateInnerContext(PreCreationContext context) => context;

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, PreCreationContext innerContext) => innerContext;
}