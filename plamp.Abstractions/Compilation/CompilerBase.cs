using plamp.Abstractions.FileLoading;

namespace plamp.Abstractions.Compilation;

public abstract class BaseCompiler<TLoaderFactory, TLoader>
    where TLoaderFactory : IFileLoaderFactory<TLoader>
    where TLoader : FileLoaderBase
{
    protected TLoaderFactory LoaderFactory { get; }

    public BaseCompiler(TLoaderFactory loaderFactory)
    {
        LoaderFactory = loaderFactory;
    }

    public abstract TLoader CreateCompilation();
}