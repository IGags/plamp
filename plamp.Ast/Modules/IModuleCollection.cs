using System.Collections.Generic;

namespace plamp.Ast.Modules;

public interface IModuleCollection
{
    /// <summary>
    /// Returns module if exists
    /// </summary>
    /// <param name="name">Requested module name</param>
    /// <param name="module">Out module themselves</param>
    /// <returns>Success flag</returns>
    public bool TryGetModule(string name, out IModuleDefinition module);
    
    /// <summary>
    /// All added modules
    /// </summary>
    public IReadOnlyList<IModuleDefinition> Modules { get; }
}