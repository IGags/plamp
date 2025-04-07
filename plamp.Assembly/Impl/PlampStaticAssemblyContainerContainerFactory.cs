using plamp.Abstractions;
using plamp.Assembly.Impl.Builders;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Impl;

public class PlampStaticAssemblyContainerContainerFactory 
    : IStaticAssemblyContainerFactory<PlampStaticAssemblyContainerBuilder>
{
    public PlampStaticAssemblyContainerBuilder CreateBuilder() => new();
    public ResourceType Type => ResourceType.InstancePerRequest;
}