namespace plamp.Ast.Assemblies;

public interface IStaticAssemblyContainerBuilder
{
    //TODO: Shared api for libs
    public IStaticAssemblyContainer Build();
}