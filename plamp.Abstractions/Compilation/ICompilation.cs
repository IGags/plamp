using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Compilation.Models;

namespace plamp.Abstractions.Compilation;

public interface ICompilation<TResult>
{
    void AddDynamicAssembly(AssemblyName assemblyName, HashSet<SourceFile> sourceFile, HashSet<AssemblyName> referencedAssemblies = null);

    Task<TResult> CompileAsync(CancellationToken cancellationToken = default);
}