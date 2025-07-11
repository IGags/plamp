using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.AstExtensions;

namespace plamp.Alternative.Visitors;

public class CompilationWeaver : BaseExtendedWeaver<CompilationContext, CompilationContext, CompilationResult>
{
    protected override CompilationContext CreateInnerContext(CompilationContext context) => context;

    protected override CompilationResult CreateWeaveResult(CompilationContext innerContext, CompilationContext outerContext) => new();

    protected override VisitResult VisitDef(DefNode node, CompilationContext context)
    {
    }
}

public record CompilationContext(List<MethodBuilder> Builders);
public record CompilationResult;