namespace plamp.Abstractions;

public interface ICompilerEntity
{
    bool CanReuseCreated { get; }
    
    bool CanParallelCreated { get; }
}