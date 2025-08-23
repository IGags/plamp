using System;
using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Alternative;

internal static class TypeResolveHelper
{
    public static Type? ResolveType(TypeNode type, List<PlampException> exceptions, ISymbolTable symbols, string fileName)
    {
        switch (type.TypeName.Name)
        {
            case "int": return typeof(int);
            case "uint": return typeof(uint);
            case "long": return typeof(long);
            case "ulong": return typeof(ulong);
            case "char": return typeof(char);
            case "byte": return typeof(byte);
            case "float": return typeof(float);
            case "double": return typeof(double);
            case "bool": return typeof(bool);
            case "string": return typeof(string);
            case "any": return typeof(object);
        }

        var record = PlampExceptionInfo.TypesIsNotSupported();
        exceptions.Add(symbols.SetExceptionToNode(type, record, fileName));
        return null;
    }
    
    public static MethodInfo? TryGetIntrinsic(string intrinsicName)
    {
        return intrinsicName switch
        {
            "println" => typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(object)]),
            "readln" => typeof(Console).GetMethod(nameof(Console.ReadLine), []),
            "int" => typeof(int).GetMethod(nameof(int.Parse), [typeof(string)]),
            _ => null
        };
    }
}