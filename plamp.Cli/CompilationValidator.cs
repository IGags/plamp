using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Abstractions.CompilerEmission;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.Cli;

public class CompilationValidator : BaseValidator<CreationContext, InnerCompilationContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, InnerCompilationContext context, NodeBase? parent)
    {
        var builder = context.Methods.Single(x => x.Name == node.Name.MemberName);
        var emitter = new DefaultIlCodeEmitter();
        var dbg = new DebugMethodBuilder(builder);
        var emissionContext = new CompilerEmissionContext(
            node.Body,
            dbg,
            context.FuncParams[node.Name.MemberName], context.SymbolTable);
        emitter.EmitMethodBodyAsync(emissionContext).Wait();
        Console.WriteLine(dbg.GetIlRepresentation());
        return VisitResult.SkipChildren;
    }

    protected override InnerCompilationContext CreateInnerContext(CreationContext context) => new(context);

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, InnerCompilationContext innerContext) => new(innerContext);
}

public class InnerCompilationContext : CreationContext
{
    public Dictionary<string, ParameterInfo[]> FuncParams { get; }

    public InnerCompilationContext(CreationContext other) : base(other)
    {
        FuncParams = other.Functions.ToDictionary(
            x => x.Key, 
            x => x.Value.ParameterList
                .Select(y => new ParamImpl(y.Type.Symbol!, y.Name.MemberName))
                .Cast<ParameterInfo>().ToArray());
    }
}