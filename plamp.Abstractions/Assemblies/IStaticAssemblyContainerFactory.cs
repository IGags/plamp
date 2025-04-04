namespace plamp.Abstractions.Assemblies;

public interface IStaticAssemblyContainerFactory<out TBuilder> : ICompilerEntity
    where TBuilder : IStaticAssemblyContainerBuilder
{
    public TBuilder CreateBuilder();
}