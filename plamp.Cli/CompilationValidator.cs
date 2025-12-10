using System.Reflection;
using plamp.Abstractions;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.Cli;

public class CompilationValidator : BaseValidator<CreationContext, InnerCompilationContext>
{
    protected override VisitResult PreVisitFunction(FuncNode node, InnerCompilationContext context, NodeBase? parent)
    {
        var builder = context.Methods.Single(x => x.Name == node.FuncName.Value);
        var overload = context.SymbolTable.GetMatchingFunction(
            node.FuncName.Value, 
            node.ParameterList.Select(x => x.Type.TypedefRef).ToList());

        if (overload == null)
        {
            throw new Exception();
        }
        
        
        var dbg = new DebugMethodBuilder(builder);
        var emissionContext = new CompilerEmissionContext(
            node.Body,
            dbg,
            context.FuncParams[overload], context.TranslationTable);
        IlCodeEmitter.EmitMethodBody(emissionContext);
        Console.WriteLine(dbg.GetIlRepresentation());
        return VisitResult.SkipChildren;
    }

    protected override InnerCompilationContext CreateInnerContext(CreationContext context) => new(context);

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, InnerCompilationContext innerContext) => new(innerContext);
}

public class InnerCompilationContext : CreationContext
{
    public Dictionary<ICompileTimeFunction, ParameterInfo[]> FuncParams { get; }

    public InnerCompilationContext(CreationContext other) : base(other)
    {
        FuncParams = other.SymbolTable.ListFunctions().ToDictionary(
            x => x, 
            x => x.GetDefinitionInfo().ArgumentList
                .Select(y => new ParamImpl(y.GetDefinitionInfo().ClrType!, y.TypeName))
                .Cast<ParameterInfo>().ToArray());
    }
}