using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.CompilerEmission;
using plamp.Alternative.AstExtensions;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.Cli;

public class CompilationWeaver : BaseExtendedWeaver<CompilationContext, CompilationContext, CompilationResult>
{
    protected override CompilationContext CreateInnerContext(CompilationContext context) => context;

    protected override CompilationResult MapInnerToOuter(CompilationContext innerContext, CompilationContext outerContext) => new();

    protected override VisitResult VisitDef(DefNode node, CompilationContext context)
    {
        var builder = context.Builders.Single(x => x.Name == node.Name.MemberName);
        var emitter = new DefaultIlCodeEmitter();
        var dbg = new DebugMethodBuilder(builder);
        var emissionContext = new CompilerEmissionContext(
            node.Body,
            dbg,
            context.FuncParams[node.Name.MemberName], null, null);
        emitter.EmitMethodBodyAsync(emissionContext).Wait();
        Console.WriteLine(dbg.GetIlRepresentation());
        return VisitResult.SkipChildren;
    }
}

public record CompilationContext(List<MethodBuilder> Builders, Dictionary<string, ParameterInfo[]> FuncParams);
public record CompilationResult;