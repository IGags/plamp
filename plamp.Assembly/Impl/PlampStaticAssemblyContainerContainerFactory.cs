using plamp.Assembly.Impl.Builders;
using plamp.Ast.Assemblies;

namespace plamp.Assembly.Impl;

public class PlampStaticAssemblyContainerContainerFactory 
    : IStaticAssemblyContainerFactory<PlampStaticAssemblyContainerBuilder>
{
    public PlampStaticAssemblyContainerBuilder CreateBuilder() => new();
    
    
}