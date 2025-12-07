using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Alternative;

internal static class TypeResolveHelper
{
    public static PlampExceptionRecord? FindTypeByName(
        string name, 
        List<ArrayTypeSpecificationNode> arrayDefs,
        IEnumerable<ISymbolTable> symbolTables, 
        out ICompileTimeType? typeRef)
    {
        typeRef = null;
        var types = new List<ICompileTimeType>();
        foreach (var table in symbolTables)
        {
            table.TryGetTypeByName(name, arrayDefs, out var type);
            if(type != null) types.Add(type);
        }

        if (types.Count == 0) return PlampExceptionInfo.TypeIsNotFound(name);
        if (types.Count > 1)
        {
            return PlampExceptionInfo.AmbigulousTypeName(name, types.Select(x => x.DeclaringTable.ModuleName));
        }

        typeRef = types[0];
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
            var found = symbolTable.GetMatchingFunctions(name, argTypes);
            //Так как модуль валилидруется перед компиляцией и дубликаты сигнатур недопустимы, то выбираем самую близкую сигнатуру.
            if(found.Length > 0) funcs.Add(found[0]);
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