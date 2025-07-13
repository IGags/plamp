using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.AstExtensions;

namespace plamp.Alternative.Visitors;

public class MethodCallInferenceWeaver : BaseExtendedWeaver<MethodCallInferenceContext, MethodCallInferenceContext, MethodCallInferenceResult>
{
    protected override MethodCallInferenceContext CreateInnerContext(MethodCallInferenceContext context) => context;

    protected override MethodCallInferenceResult MapInnerToOuter(MethodCallInferenceContext innerContext,
        MethodCallInferenceContext outerContext)
    {
        return new();
    }

    protected override VisitResult VisitCall(CallNode node, MethodCallInferenceContext context)
    {
        var info = TypeResolveHelper.TryGetIntrinsic(node.MethodName.MemberName);
        var fromContext = context.Methods.FirstOrDefault(x => x.Name == node.MethodName.MemberName);
        if (fromContext != null) info = fromContext;
        node.SetInfo(info);
        return VisitResult.Continue;
    }
}

public record MethodCallInferenceContext(List<MethodBuilder> Methods, SymbolTable SymbolTable);
public record MethodCallInferenceResult;