using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using plamp.Abstractions.Symbols.SymTable;
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

    public static IFnInfo StrConcat => SymTable.FindFunc(BuiltinSymTable.StrConcatName)!;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(string? str)
    {
        return str?.Length ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(Array? arr)
    {
        return arr?.Length ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Concat(string arg1, string arg2) => arg1 + arg2;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ArrayEquals(Array? arr1, Array? arr2)
    {
        var firstEmpty = arr1 == null || arr1.Length == 0;
        var secondEmpty = arr2 == null || arr2.Length == 0;
        
        if (firstEmpty && secondEmpty) return true;
        if (firstEmpty ^ secondEmpty) return false;

        var fstType = arr1!.GetType().GetElementType()!;
        var sndType = arr2!.GetType().GetElementType()!;
        if (fstType != sndType || arr1.Length != arr2.Length) return false;
        
        if (fstType == typeof(string))
        {
            for (var i = 0; i < arr1.Length; i++)
            {
                if (!StringEquals(arr1.GetValue(i) as string, arr2.GetValue(i) as string)) return false;
            }

            return true;
        }

        if (fstType.IsArray)
        {
            for (var i = 0; i < arr1.Length; i++)
            {
                if (!ArrayEquals(arr1.GetValue(i) as Array, arr2.GetValue(i) as Array)) return false;
            }

            return true;
        }

        if (!fstType.IsValueType) throw new Exception();
        
        for (var i = 0; i < arr1.Length; i++)
        {
            if (arr1.GetValue(i)!.Equals(arr2.GetValue(i))) return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StringEquals(string? str1, string? str2)
    {
        var firstEmpty = string.IsNullOrEmpty(str1);
        var secondEmpty = string.IsNullOrEmpty(str2);
        
        if (firstEmpty && secondEmpty) return true;
        if (firstEmpty ^ secondEmpty) return false;

        return str1!.Equals(str2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AnyEquals(object? fst, object? snd)
    {
        if (fst == null && snd == null) return true;
        if (fst == null ^ snd == null) return false;

        var fstType = fst!.GetType();
        var sndType = snd!.GetType();

        if (fstType != sndType) return false;

        if (fstType.IsArray) return ArrayEquals(fst as Array, snd as Array);
        if (fstType == typeof(string)) return StringEquals(fst as string, snd as string);

        if (!fstType.IsValueType) throw new Exception();
        
        return fst.Equals(snd);
    }
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

    internal const string StrConcatName = "strConcat";
    
    private readonly FrozenDictionary<string, ITypeInfo> _types;
    private readonly FrozenDictionary<string, FuncInfo> _funcs;
    
    public string ModuleName => "<builtins>";

    public BuiltinSymTable()
    {
        var typeDict = new Dictionary<string, ITypeInfo>()
        {
            [IntName] = TypeInfo.FromType(typeof(int), ModuleName, IntName),
            [UintName] = TypeInfo.FromType(typeof(uint), ModuleName, UintName),
            [LongName] = TypeInfo.FromType(typeof(long), ModuleName, LongName),
            [UlongName] = TypeInfo.FromType(typeof(ulong), ModuleName, UlongName),
            [ShortName] = TypeInfo.FromType(typeof(short), ModuleName, ShortName),
            [UshortName] = TypeInfo.FromType(typeof(ushort), ModuleName, UshortName),
            [ByteName] = TypeInfo.FromType(typeof(byte), ModuleName, ByteName),
            [SbyteName] = TypeInfo.FromType(typeof(sbyte), ModuleName, SbyteName),
            [FloatName] = TypeInfo.FromType(typeof(float), ModuleName, FloatName),
            [DoubleName] = TypeInfo.FromType(typeof(double), ModuleName, DoubleName),
            [BoolName] = TypeInfo.FromType(typeof(bool), ModuleName, BoolName),
            [StringName] = TypeInfo.FromType(typeof(string), ModuleName, StringName),
            [CharName] = TypeInfo.FromType(typeof(char), ModuleName, CharName),
            [AnyName] = TypeInfo.FromType(typeof(object), ModuleName, AnyName),
            [ArrayName] = TypeInfo.FromType(typeof(Array), ModuleName, ArrayName),
            [VoidName] = TypeInfo.FromType(typeof(void), ModuleName, VoidName)
        };
        _types = typeDict.ToFrozenDictionary();
        
        const string printName = "print";
        const string printlnName = "println";
        const string readName = "read";
        const string readlnName = "readln";
        const string strLenName = "strLen";
        const string arrLenName = "arrLen";
        const string arrConcatName = "arrConcat";
        const string arrEqualsName = "arrEquals";
        const string strEqualsName = "strEquals";
        
        var funcDict = new Dictionary<string, FuncInfo>()
        {
            [printName] = new(typeof(Console).GetMethod(nameof(Console.Write), [typeof(object)])!, ModuleName, printName),
            [printlnName] = new(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(object)])!, ModuleName, printlnName),
            [readName] = new(typeof(Console).GetMethod(nameof(Console.Read), [])!, ModuleName, readName),
            [readlnName] = new(typeof(Console).GetMethod(nameof(Console.ReadLine), [])!, ModuleName, readlnName),
            [strLenName] = new(typeof(Builtins).GetMethod(nameof(Builtins.Length), [typeof(string)])!, ModuleName, strLenName),
            [arrLenName] = new(typeof(Builtins).GetMethod(nameof(Builtins.Length), [typeof(Array)])!, ModuleName, arrLenName),
            [arrConcatName] = new(typeof(string).GetMethod(nameof(string.Concat), [typeof(string[])])!, ModuleName, arrConcatName),
            [StrConcatName] = new(typeof(string).GetMethod(nameof(Builtins.Concat), [typeof(string), typeof(string)])!, ModuleName, StrConcatName),
            [arrEqualsName] = new(typeof(Builtins).GetMethod(nameof(Builtins.ArrayEquals), [typeof(Array), typeof(Array)])!, ModuleName, arrEqualsName),
            [strEqualsName] = new(typeof(Builtins).GetMethod(nameof(Builtins.StringEquals), [typeof(string), typeof(string)])!, ModuleName, strEqualsName),
        };
        _funcs = funcDict.ToFrozenDictionary();
    }

    public ITypeInfo? FindType(string name)
    {
        name = name.Equals(string.Empty) ? VoidName : name;
        return _types.GetValueOrDefault(name);
    }

    public IFnInfo? FindFunc(string name) => _funcs.GetValueOrDefault(name);
    
    public bool ContainsSymbol(string name) => _funcs.ContainsKey(name) || _types.ContainsKey(name);
}