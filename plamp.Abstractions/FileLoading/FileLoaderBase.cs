using plamp.Abstractions.Compilation;

namespace plamp.Abstractions.FileLoading;

public abstract class FileLoaderBase
{
    protected ICompilation Compilation { get; }

    protected FileLoaderBase(ICompilation compilation)
    {
        Compilation = compilation;
    }
}