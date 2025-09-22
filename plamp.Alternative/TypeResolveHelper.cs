using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Intrinsics;

namespace plamp.Alternative;

internal static class TypeResolveHelper
{
    public static Type? ResolveType(TypeNode type, List<PlampException> exceptions, ISymbolTable symbols, string fileName)
    {
        var typ = type.TypeName.Name switch
        {
            "int" => typeof(int),
            "uint" => typeof(uint),
            "long" => typeof(long),
            "ulong" => typeof(ulong),
            "char" => typeof(char),
            "byte" => typeof(byte),
            "float" => typeof(float),
            "double" => typeof(double),
            "bool" => typeof(bool),
            "string" => typeof(string),
            "any" => typeof(object),
            _ => null
        };
        
        if (typ == null)
        {
            var record = PlampExceptionInfo.TypesIsNotSupported();
            exceptions.Add(symbols.SetExceptionToNode(type, record, fileName));
            return null;
        }

        typ = MakeArrayFromType(typ, type.ArrayDefinitions);
        return typ;
    }
    
    public static MethodInfo? TryGetIntrinsic(string intrinsicName, Type[] argTypes)
    {
        return intrinsicName switch
        {
            "println" => TryGetPrintln(argTypes),
            "print"   => TryGetPrint(argTypes),
            "readln"  => TryGetReadln(argTypes),
            "read"    => TryGetRead(argTypes),
            "length"  => TryGetLength(argTypes),
            "int" => typeof(int).GetMethod(nameof(int.Parse), [typeof(string)]),
            _ => null
        };
    }

    private static MethodInfo? TryGetPrint(Type[] argTypes) 
        => typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), argTypes);

    private static MethodInfo? TryGetPrintln(Type[] argTypes) 
        => typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), argTypes);

    private static MethodInfo? TryGetRead(Type[] argTypes) 
        => typeof(ReadIntrinsics).GetMethod(nameof(ReadIntrinsics.Read), argTypes);

    private static MethodInfo? TryGetReadln(Type[] argTypes) 
        => typeof(ReadIntrinsics).GetMethod(nameof(ReadIntrinsics.Readln), argTypes);

    private static MethodInfo? TryGetLength(Type[] argTypes)
    {
        if (argTypes.Length != 1 || !argTypes[0].IsAssignableTo(typeof(Array))) return null;
        return argTypes[0] == typeof(string) 
            ? typeof(LengthIntrinsics).GetMethod(nameof(LengthIntrinsics.Length), [typeof(string)])
            : typeof(LengthIntrinsics).GetMethod(nameof(LengthIntrinsics.Length), [typeof(Array)]);
    }

    private static Type MakeArrayFromType(Type originalType, List<ArrayTypeSpecificationNode> arrayDefs) 
        => arrayDefs.Aggregate(originalType, (current, def) => current.MakeArrayType(def.Dimensions));
}