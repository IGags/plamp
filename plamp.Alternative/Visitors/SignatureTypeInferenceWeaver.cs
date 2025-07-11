using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.AstExtensions;

namespace plamp.Alternative.Visitors;

/// <summary>
/// Определяет только типы аргументов
/// </summary>
public class SignatureTypeInferenceWeaver : BaseExtendedWeaver<SignatureInferenceContext, InnerSignatureInferenceContext, SignatureInferenceResult>
{
    protected override InnerSignatureInferenceContext CreateInnerContext(SignatureInferenceContext context)
    {
        return new InnerSignatureInferenceContext([], context.Exceptions, context.SymbolTable, context.FileName);
    }

    protected override SignatureInferenceResult CreateWeaveResult(InnerSignatureInferenceContext innerContext, SignatureInferenceContext outerContext)
    {
        return new SignatureInferenceResult(innerContext.Signatures, innerContext.Exceptions);
    }

    protected override VisitResult VisitDef(DefNode node, InnerSignatureInferenceContext context)
    {
        var returnType = node.ReturnType;
        var actualReturnType = TypeResolveHelper.ResolveType(returnType, context.Exceptions, context.SymbolTable, context.FileName);
        
        if (actualReturnType != null)
        {
            var newType = new TypeNode(returnType.TypeName, []) { Symbol = actualReturnType };
            ReplaceNode(node, returnType, newType, context);
        }

        foreach (var parameter in node.ParameterList)
        {
            var parameterType = parameter.Type;
            var actualParameterType = TypeResolveHelper.ResolveType(parameterType, context.Exceptions,
                context.SymbolTable, context.FileName);
            if (actualParameterType == null) continue;
            var newParameterType = new TypeNode(parameterType.TypeName, []) { Symbol = actualParameterType };
            ReplaceNode(parameter, parameterType, newParameterType, context);
        }
        
        context.Signatures.Add(node);
        return VisitResult.SkipChildren;
    }
    

    private void ReplaceNode(NodeBase parent, NodeBase oldNode, NodeBase newNode, InnerSignatureInferenceContext context)
    {
        context.SymbolTable.ReplaceSymbol(oldNode, newNode);
        parent.ReplaceChild(oldNode, newNode);
    }
}

public record SignatureInferenceContext( 
    List<PlampException> Exceptions,
    SymbolTable SymbolTable,
    string FileName);

public record InnerSignatureInferenceContext(
    List<DefNode> Signatures,
    List<PlampException> Exceptions,
    SymbolTable SymbolTable,
    string FileName);
    
public record SignatureInferenceResult(
    List<DefNode> Signatures,
    List<PlampException> Exceptions
    );