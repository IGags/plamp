namespace plamp.Ast.Assemblies;

public interface IStaticAssemblyContainerFactory<T> where T : IStaticAssemblyContainerBuilder
{
    public T CreateBuilder();
}