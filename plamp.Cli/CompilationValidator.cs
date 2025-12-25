using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.Cli;

public class CompilationValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, CreationContext context, NodeBase? parent)
    {
        var builder = node.Func;
        var parameters = node.ParameterList.Select(x => x.ParamInfo?.AsInfo()).ToArray();
        if (builder == null || parameters.Any(x => x == null))
        {
            throw new Exception();
        }
        
        //var dbg = new DebugMethodBuilder(builder);
        var emissionContext = new CompilerEmissionContext(
            node.Body,
            builder,
            parameters!, 
            context.TranslationTable);
        IlCodeEmitter.EmitMethodBody(emissionContext);
        //Console.WriteLine(dbg.GetIlRepresentation());
        return VisitResult.SkipChildren;
    }

    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;
}