using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Compilation.Models.ApiGeneration;

namespace plamp.Abstractions.CompilerEmission;

public interface IIlCodeEmitter
{
    Task<GeneratorPair> EmitMethodBodyAsync(
        MethodEmitterPair currentEmitterPair,
        ICompiledAssemblyContainer compiledAssemblyContainer, 
        ISymbolTable symbolTable,
        CancellationToken cancellationToken = default);
}