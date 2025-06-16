using plamp.Abstractions.Compilation;

namespace plamp.Abstractions;

public interface ICompilerEntity
{
    ResourceType Type { get; }
}