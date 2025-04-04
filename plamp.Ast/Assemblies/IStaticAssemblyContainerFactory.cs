namespace plamp.Ast.Assemblies;

public interface IStaticAssemblyFactory<T> where T : IStaticAssemblyContainerBuilder
{
    public T CreateContainer();
}