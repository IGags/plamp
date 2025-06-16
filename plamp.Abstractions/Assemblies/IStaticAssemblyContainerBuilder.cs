namespace plamp.Abstractions.Assemblies;

public interface IStaticAssemblyContainerBuilder
{
    //TODO: Shared api for libs
    public ICompiledAssemblyContainer Build();
}