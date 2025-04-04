namespace plamp.Abstractions.CompilerEmission;

public interface ICompiledEmitterFactory : ICompilerEntity
{
    ICompiledEmitter CreateCompiledEmitter(string source);
}