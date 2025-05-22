using plamp.Abstractions;
using plamp.Assembly.Impl.Builders;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Compilation;

namespace plamp.Assembly.Impl;

public class PlampStaticAssemblyContainerContainerFactory 
    : IStaticAssemblyContainerFactory<PlampStaticAssemblyContainerBuilder>
{
    public PlampStaticAssemblyContainerBuilder CreateBuilder() => new();
    public ResourceType Type => ResourceType.Disposable;
}