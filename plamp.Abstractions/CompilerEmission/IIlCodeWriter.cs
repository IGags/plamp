using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Compilation.Models.ApiGeneration;

namespace plamp.Abstractions.CompilerEmission;

public interface IIlCodeWriter
{
    Task EmitToAssemblyAsync(IReadOnlyList<GeneratorPair> pairs, CancellationToken cancellationToken = default);
    
    Task CompleteCompilationAsync(AssemblyApiGenerators generators, CancellationToken cancellationToken = default);
}