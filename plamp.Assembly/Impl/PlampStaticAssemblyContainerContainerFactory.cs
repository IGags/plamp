using plamp.Assembly.Impl.Builders;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Impl;

public class PlampStaticAssemblyContainerContainerFactory 
    : IStaticAssemblyContainerFactory<PlampStaticAssemblyContainerBuilder>
{
    public PlampStaticAssemblyContainerBuilder CreateBuilder() => new();

    public bool CanReuseCreated => false;
    
    public bool CanParallelCreated => false;
}