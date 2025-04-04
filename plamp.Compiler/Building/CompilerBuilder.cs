using plamp.Ast.FileLoading;

namespace plamp.Compiler.Building;

public class CompilerBuilder
{
    private IFileLoader<ICompilation> _fileLoader;
    
    public CompilerBuilder WithFileLoader<TFileLoader, TCompilation>()
        where TFileLoader : IFileLoader<TCompilation>, new()
        where TCompilation : ICompilation
    {
        _fileLoader = new TFileLoader();
    }
}