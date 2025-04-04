namespace plamp.Ast.FileLoading;

public interface IFileLoader<out TCompilation> where TCompilation : ICompilation
{
    TCompilation CreateCompilation();
}