using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative;

public static class SymbolSearchUtility
{
    public static PlampExceptionRecord? TryGetTypeOrErrorRecord(
        TypeNode typeNode,
        IEnumerable<ISymTable> symbolTables,
        out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        var name = typeNode.TypeName.Name;
        var genericCt = typeNode.GenericParameters.Count;
        var error = TryGetTypeCore(name, genericCt, symbolTables, out var typeDef);
        if (error != null) return error;

        typeInfo = typeDef;
        return null;        
    }

    public static PlampExceptionRecord? TryGetTypeOrErrorRecord(
        TypedefNode typedefNode,
        IEnumerable<ISymTable> symbolTables,
        out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        var name = typedefNode.Name.Value;
        var genericCt = typedefNode.GenericParameters.Count;
        var error = TryGetTypeCore(name, genericCt, symbolTables, out var typeDef);
        if (error != null) return error;

        typeInfo = typeDef;
        return null;
    }

    private static PlampExceptionRecord? TryGetTypeCore(
        string name, 
        int genericCount, 
        IEnumerable<ISymTable> symbolTables,
        out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        var types = new List<(ITypeInfo typ, ISymTable table)>();
        foreach (var table in symbolTables)
        {
            var type = table.FindTypes(name).FirstOrDefault(x => GenericFilter(x, genericCount));
            if(type != null) types.Add((type, table));
        }

        if (types.Count == 0) return PlampExceptionInfo.TypeIsNotFound(name);
        if (types.Count > 1)
        {
            return PlampExceptionInfo.AmbiguousTypeName(name, types.Select(x => x.table.ModuleName));
        }

        typeInfo = types[0].typ;
        return null;

        bool GenericFilter(ITypeInfo info, int count) => info.GetGenericParameters().Count == count;
    }

    public static bool IsNumeric(ITypeInfo type)
    {
        if (type.IsArrayType) return false;
        return type.Equals(Builtins.Int)
               || type.Equals(Builtins.Uint)
               || type.Equals(Builtins.Long)
               || type.Equals(Builtins.Ulong)
               || type.Equals(Builtins.Short)
               || type.Equals(Builtins.Ushort)
               || type.Equals(Builtins.Byte)
               || type.Equals(Builtins.Sbyte)
               || type.Equals(Builtins.Double)
               || type.Equals(Builtins.Float);
    }

    public static bool IsLogical(ITypeInfo type) => type.Equals(Builtins.Bool);

    public static bool IsAny(ITypeInfo type) => type.Equals(Builtins.Any);

    public static bool IsVoid(ITypeInfo type) => type.Equals(Builtins.Void);

    public static bool IsString(ITypeInfo type) => type.Equals(Builtins.String);

    public static PlampExceptionRecord? TryGetFuncOrErrorRecord(
        string name,
        IReadOnlyList<ITypeInfo?> argTypes,
        IEnumerable<ISymTable> symbolTables, 
        out IFnInfo? fnInfo)
    {
        fnInfo = null;
        var funcs = new List<(string modName, IFnInfo fnInfo)>();
        foreach (var symbolTable in symbolTables)
        {
            var found = symbolTable.FindFuncs(name).Select(x => (symbolTable.ModuleName, x)).ToList();
            if(found.Count != 0) funcs.AddRange(found);
        }

        var fullMatch = new List<IFnInfo>();
        var partialMatch = new List<IFnInfo>();
        var matchedModules = new HashSet<string>();

        foreach (var (modName, func) in funcs)
        {
            matchedModules.Add(modName);
            switch (SignatureMatches(func.Arguments, argTypes))
            {
                case MatchResult.NotMatch: continue;
                case MatchResult.PartialMatch: partialMatch.Add(func); break;
                case MatchResult.FullMatch: fullMatch.Add(func); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        if (fullMatch.Count == 1)
        {
            fnInfo = fullMatch.First();
            return null;
        }
        
        var totalCount = fullMatch.Count + partialMatch.Count;
        
        if (totalCount == 0)
        {
            return PlampExceptionInfo.FunctionIsNotFound(name, argTypes);
        }
        if (totalCount > 1)
        {
            return PlampExceptionInfo.AmbiguousFunctionReference(name, argTypes, matchedModules);
        }

        fnInfo = fullMatch.FirstOrDefault() ?? partialMatch.FirstOrDefault()!;
        return null;
    }

    private enum MatchResult
    {
        NotMatch = 0,
        PartialMatch = 1,
        FullMatch = 2
    }
    
    private static MatchResult SignatureMatches(IReadOnlyList<IArgInfo> expected, IReadOnlyList<ITypeInfo?> actual)
    {
        if (expected.Count != actual.Count) return MatchResult.NotMatch;
        var matchType = MatchResult.FullMatch;
        
        for (var i = 0; i < expected.Count; i++)
        {
            var actualType = actual[i];
            var expectedType = expected[i].Type;
            
            if (actualType == null) { matchType = MatchResult.PartialMatch; continue; }
            if (expectedType.Equals(actualType)) continue;
            if (!ImplicitlyConvertable(actualType, expectedType)) return MatchResult.NotMatch;
            matchType = MatchResult.PartialMatch;
        }

        return matchType;
    }

    public static bool ImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        return ImplicitlyNumericConvertable(from, to)
               || ArrayImplicitlyConvertable(from, to)
               || AnyImplicitlyConvertable(from, to);
    }

    public static bool NeedToCreateCast(ITypeInfo from, ITypeInfo to)
    {
        return !ArrayImplicitlyConvertable(from, to)
               && !AnyImplicitlyConvertable(from, to);
    }
    
    private static bool ImplicitlyNumericConvertable(ITypeInfo from, ITypeInfo to)
    {
        if (!IsNumeric(from) || !IsNumeric(to)) return false;
        var fromPower = GetNumericTypeConversionPower(from);
        var toPower = GetNumericTypeConversionPower(to);
        var difference = fromPower - toPower;
        return difference > 0;
    }

    private static bool ArrayImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        return to.Equals(Builtins.Array) && from.IsArrayType;
    }

    private static bool AnyImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        return to.Equals(Builtins.Any) && !from.Equals(Builtins.Void);
    }

    private static int GetNumericTypeConversionPower(ITypeInfo type)
    {
        if (type.Equals(Builtins.Double)) return 0;
        if (type.Equals(Builtins.Float))  return 1;
        if (type.Equals(Builtins.Long) || type.Equals(Builtins.Ulong)) return 2;
        if (type.Equals(Builtins.Int) || type.Equals(Builtins.Uint)) return 3;
        if (type.Equals(Builtins.Short) || type.Equals(Builtins.Ushort)) return 4;
        if (type.Equals(Builtins.Byte) || type.Equals(Builtins.Sbyte)) return 5;
        return int.MaxValue;
    }
}