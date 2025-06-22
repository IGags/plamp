using System.Threading;
using System.Threading.Tasks;

namespace plamp.Abstractions.CompilerEmission;

public interface IIlCodeEmitter
{
    Task EmitMethodBodyAsync(CompilerEmissionContext context, CancellationToken cancellationToken = default);
}