using System.Collections.Generic;

namespace plamp.Abstractions.Symbols;

public interface ISymTable
{
    public string ModuleName { get; }
    
    public ITypeInfo? FindType(string name);

    public IReadOnlyList<IFnInfo> FindFuncs(string name);
}