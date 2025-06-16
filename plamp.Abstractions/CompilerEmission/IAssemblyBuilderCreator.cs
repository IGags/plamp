using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace plamp.Abstractions.CompilerEmission;

public interface IAssemblyBuilderCreator
{
    Task<AssemblyBuilder> CreateAssemblyBuilderAsync(
        AssemblyName assemblyName, 
        string moduleName = null, 
        CancellationToken cancellationToken = default);
}