namespace plamp.Abstractions.FileLoading;

public interface IFileLoaderFactory<out TFileLoader> : ICompilerEntity
{
    public TFileLoader CreateFileLoader();
}