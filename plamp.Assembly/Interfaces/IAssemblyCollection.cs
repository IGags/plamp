using System.Collections.Generic;

namespace plamp.Assembly.Interfaces;

public interface IAssemblyCollection
{
    public void AddAssembly<T>() where T : IAssemblyDefinition;

    public void AddAssemblyThroughReflection<T>() where T : class;

    public IReadOnlyList<string> GetAssembliesList();

    public void Replace(string assemblyName, IAssemblyDefinition assemblyDefinition);

    public void Remove(string assemblyName);

    public IAssemblyProvider BuildAssemblyProvider();
}