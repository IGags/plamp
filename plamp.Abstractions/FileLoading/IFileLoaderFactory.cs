using plamp.Abstractions.Compilation;

namespace plamp.Abstractions.FileLoading;

public interface IFileLoaderFactory<out TFileLoader> : ICompilerEntity
    where TFileLoader : FileLoaderBase
{
    public TFileLoader CreateFileLoader(ICompilation compilation);
}