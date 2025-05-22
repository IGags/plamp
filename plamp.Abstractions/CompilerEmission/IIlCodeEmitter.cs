using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.CompilerEmission;

public interface IIlCodeEmitter
{
    Task EmitMethodBodyAsync(CompilerEmissionContext context, CancellationToken cancellationToken = default);
}