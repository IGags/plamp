using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

public class SymTable : ISymTable
{
    private readonly FrozenDictionary<string, ITypeInfo> _types;

    private readonly FrozenDictionary<string, IReadOnlyList<IFnInfo>> _funcs;
    
    public string ModuleName { get; }
    
    public SymTable(string moduleName, List<Type> types, List<MethodInfo> funcs)
    {
        var typeDistinct = types.Select(x => x.Name).Distinct().Count();
        if (types.Count != typeDistinct) throw new Exception("Symbol table cannot has types with duplicate names");
        _types = types.ToDictionary(x => x.Name, ITypeInfo (x) => new TypeInfo(x)).ToFrozenDictionary();
        _funcs = PrepareFuncs(funcs);
        ModuleName = moduleName;
    }

    private FrozenDictionary<string, IReadOnlyList<IFnInfo>> PrepareFuncs(List<MethodInfo> funcs)
    {
        var overloadGrouping = funcs.GroupBy(x => x.Name);

        return overloadGrouping
            .Select(PrepareOverloads)
            .ToDictionary(x => x.First().Name, IReadOnlyList<IFnInfo> (x) => x)
            .ToFrozenDictionary();
    }

    private List<IFnInfo> PrepareOverloads(IEnumerable<MethodInfo> overloads)
    {
        var comparer = StructuralComparisons.StructuralEqualityComparer;
        var overloadList = overloads.ToList();
        for (var i = 0; i < overloadList.Count - 1; i++)
        {
            for (var j = i + 1; i < overloadList.Count; j++)
            {
                if (!comparer.Equals(overloadList[i], overloadList[j])) continue;
                throw new Exception("Symbol table cannot has overloads with duplicate signatures");
            }
        }

        return overloadList.Select(IFnInfo (x) => new FuncInfo(x)).ToList();
    }

    
    public ITypeInfo? FindType(string name) => _types.GetValueOrDefault(name);

    public IReadOnlyList<IFnInfo> FindFuncs(string name)
    {
        return _funcs.TryGetValue(name, out var overloads) ? overloads : [];
    }
}
