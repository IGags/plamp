using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative;

internal static class SymbolSearchUtility
{
    public static PlampExceptionRecord? TryGetTypeOrErrorRecord(
        string name,
        IEnumerable<ISymTable> symbolTables,
        out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        var types = new List<(ITypeInfo typ, ISymTable table)>();
        foreach (var table in symbolTables)
        {
            var type = table.FindType(name);
            if(type != null) types.Add((type, table));
        }

        if (types.Count == 0) return PlampExceptionInfo.TypeIsNotFound(name);
        if (types.Count > 1)
        {
            return PlampExceptionInfo.AmbigulousTypeName(name, types.Select(x => x.table.ModuleName));
        }

        typeInfo = types[0].typ;
        return null;
    }

    public static PlampExceptionRecord? FindFuncBySignature(
        string name, 
        IReadOnlyList<ICompileTimeType?> argTypes,
        IEnumerable<ISymbolTable> symbolTables, 
        out ICompileTimeFunction? funcRef)
    {
        funcRef = null;
        var funcs = new List<ICompileTimeFunction>();
        foreach (var symbolTable in symbolTables)
        {
            var found = symbolTable.GetMatchingFunction(name, argTypes);
            //Так как модуль валилидруется перед компиляцией и дубликаты сигнатур недопустимы, то выбираем самую близкую сигнатуру.
            if(found != null) funcs.Add(found);
        }

        if (funcs.Count == 0)
        {
            return PlampExceptionInfo.FunctionIsNotFound(name, argTypes);
        }
        if (funcs.Count > 1)
        {
            return PlampExceptionInfo.AmbigulousFunctionReference(name, argTypes, funcs.Select(x => x.DeclaringTable.ModuleName));
        }

        funcRef = funcs[0];
        return null;
    }
}