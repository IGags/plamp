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
            var type = table.FindType(name);
            if(type != null) types.Add((type, table));
        }

        if (types.Count > 1)
        {
            return PlampExceptionInfo.AmbiguousTypeName(name, types.Select(x => x.table.ModuleName));
        }
        if (types.Count == 0) return PlampExceptionInfo.TypeIsNotFound(name);

        var defParamCount = types[0].typ.GetGenericParameters().Count; 
        if (defParamCount != genericCount)
        {
            return PlampExceptionInfo.GenericTypeDefinitionHasDifferentParameterCount(defParamCount, genericCount);
        }
        
        typeInfo = types[0].typ;
        return null;
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
        IEnumerable<ISymTable> symbolTables, 
        out IFnInfo? fnInfo)
    {
        fnInfo = null;
        var funcs = new List<(string modName, IFnInfo fnInfo)>();
        foreach (var symbolTable in symbolTables)
        {
            var found = symbolTable.FindFunc(name);
            if(found != null) funcs.Add((symbolTable.ModuleName, found));
        }

        if (funcs.Count == 1) fnInfo = funcs[0].fnInfo;

        return funcs.Count switch
        {
            0 => PlampExceptionInfo.FunctionIsNotFound(name),
            > 1 => PlampExceptionInfo.AmbiguousFunctionReference(name, funcs.Select(x => x.modName)),
            _ => null
        };
    }

    public static PlampExceptionRecord? MatchArgumentOrGetError(
        ITypeInfo fnParameterType,
        ITypeInfo fnArgType,
        List<KeyValuePair<ITypeInfo, ITypeInfo>> genericMapping)
    {
        if (fnParameterType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException("В аргументе объявления функции не может быть объявления дженерик типа");
        }

        if (fnArgType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException("В аргументе функции не может быть объявления дженерик типа");
        }
        
        if (fnParameterType.IsGenericTypeParameter)
        {
            genericMapping.Add(new(fnParameterType, fnArgType));
            return null;
        }

        if (fnParameterType.IsArrayType)
        {
            if (!fnArgType.IsArrayType) return PlampExceptionInfo.CannotApplyArgument();
            
            var fnParamElem = fnParameterType.ElementType();
            ArgumentNullException.ThrowIfNull(fnParamElem);
            var fnArgElem = fnArgType.ElementType();
            ArgumentNullException.ThrowIfNull(fnArgElem);

            return MatchArgumentOrGetError(fnParamElem, fnArgElem, genericMapping);
        }
        
        if (fnParameterType.IsGenericType)
        {
            if (!fnArgType.IsGenericType) return PlampExceptionInfo.CannotApplyArgument();
            
            var fnParamDef = fnParameterType.GetGenericTypeDefinition();
            ArgumentNullException.ThrowIfNull(fnParamDef);
            var fnArgDef = fnArgType.GetGenericTypeDefinition();
            ArgumentNullException.ThrowIfNull(fnArgDef);
            
            if (!fnArgDef.Equals(fnParamDef)) return PlampExceptionInfo.CannotApplyArgument();
            
            var fnParamArgs = fnParameterType.GetGenericArguments();
            var fnArgArgs = fnArgType.GetGenericArguments();

            if (fnParamArgs.Count != fnArgArgs.Count) return PlampExceptionInfo.CannotApplyArgument();

            PlampExceptionRecord? record = null; 
            foreach (var (paramArg, argArg) in fnParamArgs.Zip(fnArgArgs))
            {
                record ??= MatchArgumentOrGetError(paramArg, argArg, genericMapping);
            }

            return record;
        }

        return ImplicitlyConvertable(fnParameterType, fnArgType) ? null : PlampExceptionInfo.CannotApplyArgument();
    }

    public static bool ImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        return ImplicitlyNumericConvertable(from, to)
               || ArrayImplicitlyConvertable(from, to)
               || AnyImplicitlyConvertable(from, to)
               || GenericParamToAnyConvertable(from, to);
    }

    public static bool NeedToCast(ITypeInfo from, ITypeInfo to)
    {
        return !ArrayImplicitlyConvertable(from, to);
    }

    private static bool GenericParamToAnyConvertable(ITypeInfo from, ITypeInfo to) 
        => from.IsGenericTypeParameter && IsAny(to);

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