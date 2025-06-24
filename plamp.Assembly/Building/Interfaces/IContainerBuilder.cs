using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Building.Interfaces;

public interface IContainerBuilder
{
    public IModuleBuilderSyntax DefineModule(string moduleName);

    public IAssemblyContainer CreateContainer();
}