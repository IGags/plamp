using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative;

internal static class TypeResolveHelper
{
    public static Type? ResolveType(TypeNode type, List<PlampException> exceptions, SymbolTable symbols, string fileName)
    {
        switch (type.TypeName.MemberName)
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
            case "object": return typeof(object);
        }

        var record = PlampNativeExceptionInfo.TypesIsNotSupported();
        exceptions.Add(symbols.CreateExceptionForSymbol(type, record, fileName));
        return null;
    }
}