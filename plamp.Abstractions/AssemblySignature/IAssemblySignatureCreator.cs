using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AssemblySignature;

public interface IAssemblySignatureCreator
{
    Task<SignatureBuildingResult> CreateAssemblySignatureAsync(
        NodeBase rootNode,
        SignatureBuildingContext context,
        CancellationToken cancellationToken = default);
}