using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsImpl;
using System.Reflection;

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(string str) => str.Length;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(Array arr) => arr.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Concat(string arg1, string arg2) => arg1 + arg2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt(object val) => (int)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt(string val) => int.Parse(val);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(object val) => (uint)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(string val) => uint.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToLong(object val) => (long)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToLong(string val) => long.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ToUlong(object val) => (ulong)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ToUlong(string val) => ulong.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ToShort(object val) => (short)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ToShort(string val) => short.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ToUshort(object val) => (ushort)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ToUshort(string val) => ushort.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToByte(object val) => (byte)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToByte(string val) => byte.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ToSbyte(object val) => (sbyte)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ToSbyte(string val) => sbyte.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ToChar(object val) => (char)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ToChar(string val) => char.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(object val) => (double)val;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(string val) => double.Parse(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToFloat(object val) => (float)val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToFloat(string val) => float.Parse(val);

    //Функции будут удалены в новых версиях
    
    public static TimeSpan FromMinutes(double val) => TimeSpan.FromMinutes(val);

    public static TimeSpan FromSeconds(double val) => TimeSpan.FromSeconds(val);

    public static TimeSpan FromHours(double val) => TimeSpan.FromHours(val);

    public static TimeSpan FromMilliseconds(double val) => TimeSpan.FromMilliseconds(val);

    public static TimeSpan FromDays(double val) => TimeSpan.FromDays(val);

    public static double TotalDays(TimeSpan val) => val.TotalDays;

    public static double TotalMilliseconds(TimeSpan val) => val.TotalMilliseconds;

    public static double TotalHours(TimeSpan val) => val.TotalHours;

    public static double TotalSeconds(TimeSpan val) => val.TotalSeconds;

    public static double TotalMinutes(TimeSpan val) => val.TotalMinutes;

    public static int Days(TimeSpan val) => val.Days;

    public static int Hours(TimeSpan val) => val.Hours;

    public static int Milliseconds(TimeSpan val) => val.Milliseconds;

    public static int Seconds(TimeSpan val) => val.Seconds;

    public static int Minutes(TimeSpan val) => val.Minutes;

    public static int Second(DateTime val) => val.Second;

    public static int Minute(DateTime val) => val.Minute;

    public static int Hour(DateTime val) => val.Hour;

    public static int Day(DateTime val) => val.Day;

    public static int Month(DateTime val) => val.Month;

    public static int Year(DateTime val) => val.Year;

    public static int DayOfYear(DateTime val) => val.DayOfYear;

    public static TimeSpan TimeOfDay(DateTime val) => val.TimeOfDay;

    public static int DayOfWeek(DateTime val) => (int)val.DayOfWeek;

    public static DateTime Now() => DateTime.Now;

    public static DateTime UtcNow() => DateTime.UtcNow;

    public static DateTime Date(DateTime date) => date.Date;

    public static TimeSpan Add(TimeSpan first, TimeSpan second) => first + second;

    public static TimeSpan Sub(TimeSpan first, TimeSpan second) => first - second;

    public static DateTime Add(DateTime date, TimeSpan interval) => date + interval;

    public static DateTime Sub(DateTime date, TimeSpan interval) => date - interval;

    public static TimeSpan Sub(DateTime first, DateTime second) => first - second;

    public static DateTime ToDate(string date) => DateTime.Parse(date);

    public static string ToString(DateTime date, string format) => date.ToString(format);

    public static string ToString(DateTime date) => date.ToString(CultureInfo.CurrentCulture);

    public static string ToString(TimeSpan interval) => interval.ToString();

    public static string ToString(TimeSpan interval, string format) => interval.ToString(format);

    public static string Substring(string str, int startIx, int length) => str.Substring(startIx, length);

    public static string Substring(string str, int startIx) => str.Substring(startIx);

    public static char Get(string str, int ix) => str[ix];

    public static bool StrCmp(string first, string second) => first.Equals(second);

    public static string ToString(object val)
    {
        if (val is TimeSpan interval) return ToString(interval);
        if (val is DateTime date) return ToString(date);
        if (val is string strVal) return strVal;
        if (val is char chrVal) return chrVal.ToString();
        if (val.GetType().IsPrimitive) return val.ToString() ?? string.Empty;
        if (val.GetType().IsArray)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            var arrVal = (Array)val;
            for (var i = 0; i < arrVal.Length; i++)
            {
                var item = arrVal.GetValue(i);
                sb.Append(item == null ? "null" : ToString(item));
                if (i + 1 < arrVal.Length) sb.Append(", ");
            }
            sb.Append(']');
            return sb.ToString();
        }

        var fields = val.GetType().GetFields().Where(x => x.GetCustomAttribute<PlampFieldGeneratedAttribute>() != null).ToList(); 
        if (fields.Count != 0)
        {
            var builder = new StringBuilder();
            var type = val.GetType();
            builder.Append(type.Name);
            builder.Append("{ ");
            for (var i = 0; i < fields.Count; i++)
            {
                builder.Append(fields[i].Name);
                builder.Append(": ");
                var value = fields[i].GetValue(val);
                if ((value?.GetType().IsPrimitive ?? false) && value is not char)
                {
                    builder.Append(value);
                }
                else if (value is char chr)
                {
                    builder.Append($"'{chr}'");
                }
                else
                {
                    builder.Append($"\"{ToString(value ?? string.Empty)}\"");
                }
                
                if (i + 1 < fields.Count) builder.Append(", ");
            }

            builder.Append(" }");

            return builder.ToString();
        }

        return val.ToString() ?? string.Empty;
    }

    public static int Length(List<object> list) => list.Count;

    public static object Get(List<object> list, int ix) => list[ix];

    public static void Append(List<object> list, object obj) => list.Add(obj);

    public static void RemoveAt(List<object> list, int ix) => list.RemoveAt(ix);

    public static void Reverse(List<object> list) => list.Reverse();

    public static List<object> Concat(List<object> first, List<object> second) => first.Concat(second).ToList();
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
    
    private readonly FrozenDictionary<string, SymbolsImpl.TypeInfo> _types;
    private readonly FrozenDictionary<string, IReadOnlyList<FuncInfo>> _funcs;
    
    public string ModuleName => "<builtins>";

    public BuiltinSymTable()
    {
        var typeDict = new Dictionary<string, SymbolsImpl.TypeInfo>()
        {
            [IntName] = new(typeof(int), IntName),
            [UintName] = new(typeof(uint), UintName),
            [LongName] = new(typeof(long), LongName),
            [UlongName] = new(typeof(ulong), UlongName),
            [ShortName] = new(typeof(short), ShortName),
            [UshortName] = new(typeof(ushort), UshortName),
            [ByteName] = new(typeof(byte), ByteName),
            [SbyteName] = new(typeof(sbyte), SbyteName),
            [FloatName] = new(typeof(float), FloatName),
            [DoubleName] = new(typeof(double), DoubleName),
            [BoolName] = new(typeof(bool), BoolName),
            [StringName] = new(typeof(string), StringName),
            [CharName] = new(typeof(char), CharName),
            [AnyName] = new(typeof(object), AnyName),
            [ArrayName] = new(typeof(Array), ArrayName),
            [VoidName] = new(typeof(void), VoidName),
            //Эти типы будут удалены в новых версиях
            ["date"] = new (typeof(DateTime), "date"),
            ["interval"] = new (typeof(TimeSpan), "interval"),
            ["AnyList"] = new (typeof(List<object>), "AnyList")
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
            new(typeof(Builtins).GetMethod(nameof(Builtins.Length), [typeof(string)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.Length), [typeof(Array)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.Length), [typeof(List<object>)])!),
        };

        var concatOverloads = new List<FuncInfo>()
        {
            new(typeof(string).GetMethod(nameof(string.Concat), [typeof(string[])])!),
            new(typeof(string).GetMethod(nameof(Builtins.Concat), [typeof(string), typeof(string)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.Concat), [typeof(List<object>), typeof(List<object>)])!)
        };

        var toIntOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToInt), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToInt), [typeof(string)])!)
        };
        
        var toUintOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToUint), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToUint), [typeof(string)])!)
        };
        
        var toLongOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToLong), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToLong), [typeof(string)])!)
        };
        
        var toUlongOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToUlong), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToUlong), [typeof(string)])!)
        };
        
        var toShortOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToShort), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToShort), [typeof(string)])!)
        };
        
        var toUshortOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToUshort), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToUshort), [typeof(string)])!)
        };
        
        var toByteOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToByte), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToByte), [typeof(string)])!)
        };
        
        var toSbyteOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToSbyte), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToSbyte), [typeof(string)])!)
        };
        
        var toFloatOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToFloat), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToFloat), [typeof(string)])!)
        };
        
        var toDoubleOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToDouble), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToDouble), [typeof(string)])!)
        };
        
        var toCharOverloads = new List<FuncInfo>()
        {
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToChar), [typeof(object)])!),
            new(typeof(Builtins).GetMethod(nameof(Builtins.ToChar), [typeof(string)])!)
        };
        
        var funcDict = new Dictionary<string, IReadOnlyList<FuncInfo>>()
        {
            ["print"] = printOverloads,
            ["println"] = printlnOverloads,
            ["read"] = [new(typeof(Console).GetMethod(nameof(Console.Read), [])!)],
            ["readln"] = [new (typeof(Console).GetMethod(nameof(Console.ReadLine), [])!)],
            ["length"] = lengthOverloads,
            ["concat"] = concatOverloads,
            ["toInt"] = toIntOverloads,
            ["toUint"] = toUintOverloads,
            ["toLong"] = toLongOverloads,
            ["toUlong"] = toUlongOverloads,
            ["toChar"] = toCharOverloads,
            ["toShort"] = toShortOverloads,
            ["toUshort"] = toUshortOverloads,
            ["toDouble"] = toDoubleOverloads,
            ["toFloat"] = toFloatOverloads,
            ["toByte"] = toByteOverloads,
            ["toSbyte"] = toSbyteOverloads,
            ["fromMinutes"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.FromMinutes), [typeof(double)])!)],
            ["fromSeconds"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.FromSeconds), [typeof(double)])!)],
            ["fromHours"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.FromHours), [typeof(double)])!)],
            ["fromMilliseconds"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.FromMilliseconds), [typeof(double)])!)],
            ["fromDays"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.FromDays), [typeof(double)])!)],
            ["totalDays"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.TotalDays), [typeof(TimeSpan)])!)],
            ["totalMilliseconds"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.TotalMilliseconds), [typeof(TimeSpan)])!)],
            ["totalHours"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.TotalHours), [typeof(TimeSpan)])!)],
            ["totalSeconds"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.TotalSeconds), [typeof(TimeSpan)])!)],
            ["totalMinutes"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.TotalMinutes), [typeof(TimeSpan)])!)],
            ["days"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Days), [typeof(TimeSpan)])!)],
            ["hours"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Hours), [typeof(TimeSpan)])!)],
            ["milliseconds"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Milliseconds), [typeof(TimeSpan)])!)],
            ["seconds"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Seconds), [typeof(TimeSpan)])!)],
            ["minutes"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Minutes), [typeof(TimeSpan)])!)],
            ["add"] = [
                new (typeof(Builtins).GetMethod(nameof(Builtins.Add), [typeof(TimeSpan), typeof(TimeSpan)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.Add), [typeof(DateTime), typeof(TimeSpan)])!)
            ],
            ["sub"] = [
                new (typeof(Builtins).GetMethod(nameof(Builtins.Sub), [typeof(TimeSpan), typeof(TimeSpan)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.Sub), [typeof(DateTime), typeof(TimeSpan)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.Sub), [typeof(DateTime), typeof(DateTime)])!),
            ],
            ["toDate"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.ToDate), [typeof(string)])!)],
            ["toString"] = [
                new (typeof(Builtins).GetMethod(nameof(Builtins.ToString), [typeof(DateTime), typeof(string)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.ToString), [typeof(DateTime)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.ToString), [typeof(TimeSpan)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.ToString), [typeof(TimeSpan), typeof(string)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.ToString), [typeof(object)])!),
            ],
            ["append"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Append), [typeof(List<object>), typeof(object)])!)],
            ["get"] = [
                new (typeof(Builtins).GetMethod(nameof(Builtins.Get), [typeof(List<object>), typeof(int)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.Get), [typeof(string), typeof(int)])!)
            ],
            ["removeAt"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.RemoveAt), [typeof(List<object>), typeof(int)])!)],
            ["reverse"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Reverse), [typeof(List<object>)])!)],
            ["second"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Second), [typeof(DateTime)])!)],
            ["minute"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Minute), [typeof(DateTime)])!)],
            ["hour"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Hour), [typeof(DateTime)])!)],
            ["day"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Day), [typeof(DateTime)])!)],
            ["month"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Month), [typeof(DateTime)])!)],
            ["year"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Year), [typeof(DateTime)])!)],
            ["dayOfYear"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.DayOfYear), [typeof(DateTime)])!)],
            ["dayOfWeek"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.DayOfWeek), [typeof(DateTime)])!)],
            ["timeOfDay"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.TimeOfDay), [typeof(DateTime)])!)],
            ["now"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Now), [])!)],
            ["utcNow"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.UtcNow), [])!)],
            ["date"] = [new (typeof(Builtins).GetMethod(nameof(Builtins.Date), [typeof(DateTime)])!)],
            ["substring"] = [
                new (typeof(Builtins).GetMethod(nameof(Builtins.Substring), [typeof(string), typeof(int), typeof(int)])!),
                new (typeof(Builtins).GetMethod(nameof(Builtins.Substring), [typeof(string), typeof(int)])!),
            ],
            ["strcmp"] = [new(typeof(Builtins).GetMethod(nameof(Builtins.StrCmp), [typeof(string), typeof(string)])!)]
        };
        _funcs = funcDict.ToFrozenDictionary();
    }

    public ITypeInfo? FindType(string name) => _types.GetValueOrDefault(name);

    public IReadOnlyList<IFnInfo> FindFuncs(string name) => _funcs.TryGetValue(name, out var list) ? list : [];
}