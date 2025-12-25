using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using plamp.Abstractions.Symbols;
using plamp.Alternative.SymbolsImpl;

namespace plamp.Alternative;

public static class Builtins
{
    public static ISymTable SymTable { get; } = new BuiltinSymTable();

    public static ITypeInfo Int => SymTable.FindType(BuiltinSymTable.IntName)!;
    public static ITypeInfo Uint => SymTable.FindType(BuiltinSymTable.UintName)!;
    public static ITypeInfo Long => SymTable.FindType(BuiltinSymTable.LongName)!;
    public static ITypeInfo Ulong => SymTable.FindType(BuiltinSymTable.UlongName)!;
    public static ITypeInfo Short => SymTable.FindType(BuiltinSymTable.ShortName)!;
    public static ITypeInfo Ushort => SymTable.FindType(BuiltinSymTable.UshortName)!;
    public static ITypeInfo Byte => SymTable.FindType(BuiltinSymTable.ByteName)!;
    public static ITypeInfo Sbyte => SymTable.FindType(BuiltinSymTable.SbyteName)!;
    public static ITypeInfo Float => SymTable.FindType(BuiltinSymTable.FloatName)!;
    public static ITypeInfo Double => SymTable.FindType(BuiltinSymTable.DoubleName)!;
    public static ITypeInfo Bool => SymTable.FindType(BuiltinSymTable.BoolName)!;
    public static ITypeInfo String => SymTable.FindType(BuiltinSymTable.StringName)!;
    public static ITypeInfo Char => SymTable.FindType(BuiltinSymTable.CharName)!;
    public static ITypeInfo Any => SymTable.FindType(BuiltinSymTable.AnyName)!;
    public static ITypeInfo Array => SymTable.FindType(BuiltinSymTable.ArrayName)!;
    public static ITypeInfo Void => SymTable.FindType(BuiltinSymTable.VoidName)!;
}

internal class BuiltinSymTable : ISymTable
{
    internal const string VoidName   = "";
    internal const string IntName    = "int";
    internal const string UintName   = "uint";
    internal const string LongName   = "long";
    internal const string UlongName  = "ulong";
    internal const string CharName   = "char";
    internal const string StringName = "string";
    internal const string ByteName   = "byte";
    internal const string BoolName   = "bool";
    internal const string ShortName  = "short";
    internal const string UshortName = "ushort";
    internal const string SbyteName  = "sbyte";
    internal const string FloatName  = "float";
    internal const string DoubleName = "double";
    internal const string AnyName    = "any";
    internal const string ArrayName  = "array";
    
    private readonly FrozenDictionary<string, TypeInfo> _types;
    private readonly FrozenDictionary<string, IReadOnlyList<FuncInfo>> _funcs;
    
    public string ModuleName => "<builtins>";

    public BuiltinSymTable()
    {
        var typeDict = new Dictionary<string, TypeInfo>()
        {
            [IntName] = new(typeof(int)),
            [UintName] = new(typeof(uint)),
            [LongName] = new(typeof(long)),
            [UlongName] = new(typeof(ulong)),
            [ShortName] = new(typeof(short)),
            [UshortName] = new(typeof(ushort)),
            [ByteName] = new(typeof(byte)),
            [SbyteName] = new(typeof(sbyte)),
            [FloatName] = new(typeof(float)),
            [DoubleName] = new(typeof(double)),
            [BoolName] = new(typeof(bool)),
            [StringName] = new(typeof(string)),
            [CharName] = new(typeof(char)),
            [AnyName] = new(typeof(object)),
            [ArrayName] = new(typeof(Array)),
            [VoidName] = new(typeof(void)),
        };
        _types = typeDict.ToFrozenDictionary();

        var printOverloads = new List<FuncInfo>()
        {
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(int)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(uint)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(long)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(ulong)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(float)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(double)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(string)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(char)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(bool)])!),
            new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(object)])!),
        };

        var printlnOverloads = new List<FuncInfo>()
        {
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(int)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(uint)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(long)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(ulong)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(float)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(double)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(string)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(char)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(bool)])!),
            new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(object)])!),
        };

        var lengthOverloads = new List<FuncInfo>()
        {
            new(typeof(Array).GetProperty(nameof(Array.Length))!.GetMethod!),
            new(typeof(string).GetProperty(nameof(string.Length))!.GetMethod!)
        };
        
        var funcDict = new Dictionary<string, IReadOnlyList<FuncInfo>>()
        {
            ["print"] = printOverloads,
            ["println"] = printlnOverloads,
            ["read"] = [new(typeof(Console).GetMethod(nameof(Console.Read), [])!)],
            ["readln"] = [new (typeof(Console).GetMethod(nameof(Console.ReadLine), [])!)],
            ["length"] = lengthOverloads
        };
        _funcs = funcDict.ToFrozenDictionary();
    }

    public ITypeInfo? FindType(string name) => _types.GetValueOrDefault(name);

    public IReadOnlyList<IFnInfo> FindFuncs(string name) => _funcs.TryGetValue(name, out var list) ? list : [];
}