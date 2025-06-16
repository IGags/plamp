using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AssemblySignature;

public interface IAssemblySignatureCreator
{
    Task<SignatureBuildingResult> CreateAssemblySignatureAsync(
        List<NodeBase> rootNodes,
        SignatureBuildingContext context,
        CancellationToken cancellationToken = default);
}