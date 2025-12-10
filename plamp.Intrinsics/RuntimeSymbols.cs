using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using plamp.Abstractions;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Intrinsics;

/// <inheritdoc cref="ISymbolTable"/>
public class RuntimeSymbols : ISymbolTable
{
    private readonly Dictionary<TypeKey, TypeDefinitionInfo> _types;
    private readonly Dictionary<ICompileTimeType, IImplicitConversionRule> _conversionRules;
    private readonly Dictionary<ICompileTimeFunction, FunctionDefinitionInfo> _funcs;
    
    public static readonly RuntimeSymbols SymbolTable = new();

    private const string VoidName   = "void";
    private const string IntName    = "int";
    private const string UintName   = "uint";
    private const string LongName   = "long";
    private const string UlongName  = "ulong";
    private const string CharName   = "char";
    private const string StringName = "string";
    private const string ByteName   = "byte";
    private const string BoolName   = "bool";
    private const string ShortName  = "short";
    private const string UshortName = "ushort";
    private const string SbyteName  = "sbyte";
    private const string FloatName  = "float";
    private const string DoubleName = "double";
    private const string AnyName    = "any";
    private const string ArrayName  = "array";

    private RuntimeSymbols()
    {
        _types = InitTypes();
        _conversionRules = InitConversionRules();
        _funcs = InitFuncs();
    }

    /// <inheritdoc/>
    public string ModuleName => "$RUNTIME$";

    public bool IsVoid(ICompileTimeType type)
    {
        if (type is not RuntimeType runtimeType) return false;
        return _types.TryGetValue(new TypeKey(runtimeType.TypeName, runtimeType.ArrayDefCount), out var value) 
               && value.ClrType == typeof(void);
    }

    public bool IsAny(ICompileTimeType type)
    {
        if (type is not RuntimeType runtimeType) return false;
        return _types.TryGetValue(new TypeKey(runtimeType.TypeName, runtimeType.ArrayDefCount), out var value)
               && value.ClrType == typeof(object);
    }

    public bool IsNumeric(ICompileTimeType type)
    {
        if (type is not RuntimeType runtimeType) return false;
        if (!_types.TryGetValue(new TypeKey(runtimeType.TypeName, runtimeType.ArrayDefCount), out var typeDef)) return false;
        var value = typeDef.ClrType;
        return value == typeof(int)
               || value == typeof(uint)
               || value == typeof(long)
               || value == typeof(ulong)
               || value == typeof(short)
               || value == typeof(ushort)
               || value == typeof(byte)
               || value == typeof(sbyte)
               || value == typeof(double)
               || value == typeof(float);
    }

    public bool IsLogical(ICompileTimeType type)
    {
        if (type is not RuntimeType runtimeType) return false;
        if (!_types.TryGetValue(new TypeKey(runtimeType.TypeName, runtimeType.ArrayDefCount), out var value)) return false;
        return value.ClrType == typeof(bool);
    }

    /// <inheritdoc/>
    public bool TryGetTypeByName(string typeName, List<ArrayTypeSpecificationNode> arrayDefs, [NotNullWhen(true)] out ICompileTimeType? type)
    {
        type = null;
        var typeKey = new TypeKey(typeName, arrayDefs.Count);
        if (_types.TryGetValue(typeKey, out var info))
        {
            type = new RuntimeType(this, info.TypeName, arrayDefs.Count);
            return true;
        }

        if (_types.TryGetValue(new TypeKey(typeName, 0), out info))
        {
            type = new RuntimeType(this, info.TypeName);
            for (var i = 0; i < arrayDefs.Count; i++)
            {
                type = type.MakeArrayType();
            }

            return true;
        }

        return false;
    }

    private RuntimeType? _void;

    public ICompileTimeType Void
    {
        get
        {
            if (_void != null) return _void;
            _void = new RuntimeType(this, VoidName);
            return _void;
        }
    }

    private RuntimeType? _bool;

    public ICompileTimeType Bool
    {
        get
        {
            if (_bool != null) return _bool;
            _bool = new RuntimeType(this, BoolName);
            return _bool;
        }
    }

    private RuntimeType? _int;
    
    public ICompileTimeType Int
    {
        get
        {
            if (_int != null) return _int;
            _int = new RuntimeType(this, IntName);
            return _int;
        }
    }

    private RuntimeType? _uint;
    
    public ICompileTimeType Uint
    {
        get
        {
            if (_uint != null) return _uint;
            _uint = new RuntimeType(this, UintName);
            return _uint;
        }
    }

    private RuntimeType? _short;
    
    public ICompileTimeType Short
    {
        get
        {
            if (_short != null) return _short;
            _short = new RuntimeType(this, ShortName);
            return _short;
        }
    }

    private RuntimeType? _ushort;
    
    public ICompileTimeType Ushort
    {
        get
        {
            if (_ushort != null) return _ushort;
            _ushort = new RuntimeType(this, UshortName);
            return _ushort;
        }
    }

    private RuntimeType? _byte;
    
    public ICompileTimeType Byte
    {
        get
        {
            if (_byte != null) return _byte;
            _byte = new RuntimeType(this, ByteName);
            return _byte;
        }
    }

    private RuntimeType? _sbyte;
    
    public ICompileTimeType Sbyte
    {
        get
        {
            if (_sbyte != null) return _sbyte;
            _sbyte = new RuntimeType(this, SbyteName);
            return _sbyte;
        }
    }

    private RuntimeType? _long;
    
    public ICompileTimeType Long
    {
        get
        {
            if (_long != null) return _long;
            _long = new RuntimeType(this, LongName);
            return _long;
        }
    }

    private RuntimeType? _ulong;
    
    public ICompileTimeType Ulong
    {
        get
        {
            if (_ulong != null) return _ulong;
            _ulong = new RuntimeType(this, UlongName);
            return _ulong;
        }
    }

    private RuntimeType? _double;
    
    public ICompileTimeType Double
    {
        get
        {
            if (_double != null) return _double;
            _double = new RuntimeType(this, DoubleName);
            return _double;
        }
    }

    private RuntimeType? _float;
    
    public ICompileTimeType Float
    {
        get
        {
            if (_float != null) return _float;
            _float = new RuntimeType(this, FloatName);
            return _float;
        }
    }

    private RuntimeType? _any;
    
    public ICompileTimeType Any
    {
        get
        {
            if (_any != null) return _any;
            _any = new RuntimeType(this, AnyName);
            return _any;
        }
    }

    private RuntimeType? _string;
    
    public ICompileTimeType String
    {
        get
        {
            if (_string != null) return _string;
            _string = new RuntimeType(this, StringName);
            return _string;
        }
    }

    private RuntimeType? _char;
    
    public ICompileTimeType Char
    {
        get
        {
            if (_char != null) return _char;
            _char = new RuntimeType(this, CharName);
            return _char;
        }
    }

    public bool TryGetFromClrType(Type clrType, [NotNullWhen(true)]out ICompileTimeType? type)
    {
        var info = _types.FirstOrDefault(x => x.Value.ClrType == clrType);
        return TryGetTypeByName(info.Key.Name, [], out type);
    }
    
    /// <inheritdoc/>
    public ICompileTimeFunction? GetMatchingFunction(string fnName, IReadOnlyList<ICompileTimeType?> signature)
    {
        var overloads = _funcs.Keys.Where(x => x.Name == fnName);
        //Сортировка по дешевизне конверсии
        var matching = overloads
            .Select(x => (x, MatchSignature(x.ArgumentTypes, signature)))
            .Where(x => x.Item2 >= 0).OrderBy(x => x.Item2).Select(x => x.x);
        return matching.FirstOrDefault();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISymbolTable> GetDependencies() => [];

    /// <summary>
    /// Метод, позволяющий проверить, что есть возможность неявной конверсии.
    /// Применяется при выводе типов. Возвращает true, если тип равен сам себе.
    /// </summary>
    /// <param name="from">Тип из которого надо проверить конверсию</param>
    /// <param name="to">Тип, в который надо проверить конверсию</param>
    /// <returns>Цена конверсии типа в целевой. -1 конверсия невозможна.</returns>
    public int TypeIsImplicitlyConvertable(ICompileTimeType from, ICompileTimeType to)
    {
        if (from.Equals(to)) return 0;
        if (!_conversionRules.TryGetValue(to, out var rule)) return -1;
        return rule.GetConversionCost(from);
    }

    /// <summary>
    /// Метод-помогатор, позволяет по правилам языка проверить, что сигнатура метода подходит с точностью до неявных преобразований типа.
    /// </summary>
    /// <param name="signatureTypes">Типы аргументов объявленной функции.</param>
    /// <param name="actualTypes">Типы аргументов, для проверки.</param>
    /// <returns>Число конверсий, которые надо совершить, чтобы получить вызов функции. Меньше - лучше. Отрицательное число значит, что вызов функции невозможен.</returns>
    public int MatchSignature(
        IReadOnlyList<ICompileTimeType> signatureTypes,
        IReadOnlyList<ICompileTimeType?> actualTypes)
    {
        if (signatureTypes.Count != actualTypes.Count) return -1;
        var conversionCost = 0;
        for (var i = 0; i < signatureTypes.Count; i++)
        {
            var type = actualTypes[i];
            if(type == null) continue;
            var cost = TypeIsImplicitlyConvertable(type, signatureTypes[i]);
            if (cost == -1) return -1;
            conversionCost += cost;
        }

        return conversionCost;
    }

    private Dictionary<TypeKey, TypeDefinitionInfo> InitTypes()
    {
        var voidDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = VoidName, DefinitionPosition = default};
        var intDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = IntName, DefinitionPosition = default};
        var uintDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = UintName, DefinitionPosition = default};
        var longDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = LongName, DefinitionPosition = default};
        var ulongDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = UlongName, DefinitionPosition = default};
        var charDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = CharName, DefinitionPosition = default};
        var stringDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = StringName, DefinitionPosition = default};
        var byteDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = ByteName, DefinitionPosition = default};
        var boolDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = BoolName, DefinitionPosition = default};
        var shortDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = ShortName, DefinitionPosition = default};
        var ushortDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = UshortName, DefinitionPosition = default};
        var sbyteDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = SbyteName, DefinitionPosition = default};
        var floatDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = FloatName, DefinitionPosition = default};
        var doubleDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = DoubleName, DefinitionPosition = default};
        var arrayDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = ArrayName, DefinitionPosition = default};
        var anyDef = new TypeDefinitionInfo{ArrayUnderlyingType = null, Fields = [], TypeName = AnyName, DefinitionPosition = default};
        voidDef.SetClrType(typeof(void));
        intDef.SetClrType(typeof(int));
        uintDef.SetClrType(typeof(uint));
        longDef.SetClrType(typeof(long));
        ulongDef.SetClrType(typeof(ulong));
        charDef.SetClrType(typeof(char));
        stringDef.SetClrType(typeof(string));
        byteDef.SetClrType(typeof(byte));
        boolDef.SetClrType(typeof(bool));
        shortDef.SetClrType(typeof(short));
        ushortDef.SetClrType(typeof(ushort));
        sbyteDef.SetClrType(typeof(sbyte));
        floatDef.SetClrType(typeof(float));
        doubleDef.SetClrType(typeof(double));
        anyDef.SetClrType(typeof(object));
        arrayDef.SetClrType(typeof(Array));
        
        return new ()
        {
            {new TypeKey(voidDef.TypeName, 0),   voidDef},
            {new TypeKey(intDef.TypeName, 0),    intDef},
            {new TypeKey(uintDef.TypeName, 0),   uintDef},
            {new TypeKey(longDef.TypeName, 0),   longDef},
            {new TypeKey(ulongDef.TypeName, 0),  ulongDef},
            {new TypeKey(charDef.TypeName, 0),   charDef},
            {new TypeKey(stringDef.TypeName, 0), stringDef},
            {new TypeKey(byteDef.TypeName, 0),   byteDef},
            {new TypeKey(boolDef.TypeName, 0),   boolDef},
            {new TypeKey(shortDef.TypeName, 0),  shortDef},
            {new TypeKey(ushortDef.TypeName, 0), ushortDef},
            {new TypeKey(sbyteDef.TypeName, 0),  sbyteDef},
            {new TypeKey(floatDef.TypeName, 0),  floatDef},
            {new TypeKey(doubleDef.TypeName, 0), doubleDef},
            {new TypeKey(anyDef.TypeName, 0),    anyDef},
            {new TypeKey(arrayDef.TypeName, 0),  arrayDef}
        };
    }

    private Dictionary<ICompileTimeType, IImplicitConversionRule> InitConversionRules()
    {
        var byteRef = (RuntimeType)Byte;
        var sbyteRef = (RuntimeType)Sbyte;
        var shortRef = (RuntimeType)Short;
        var ushortRef = (RuntimeType)Ushort;
        var intRef = (RuntimeType)Int;
        var uintRef = (RuntimeType)Uint;
        var longRef = (RuntimeType)Long;
        var ulongRef = (RuntimeType)Ulong;
        var floatRef = (RuntimeType)Float;
        var doubleRef = (RuntimeType)Double;
        var anyRef = Any;
        var voidRef = Void;
        var arrayRef = new RuntimeType(this, ArrayName);

        return new()
        {
            { doubleRef, new RuntimeImplicitConversionRule(doubleRef, [floatRef, ulongRef, longRef, uintRef, intRef, ushortRef, shortRef, sbyteRef, byteRef]) },
            { floatRef, new RuntimeImplicitConversionRule(floatRef, [ulongRef, longRef, uintRef, intRef, ushortRef, shortRef, sbyteRef, byteRef]) },
            { ulongRef, new RuntimeImplicitConversionRule(ulongRef, [uintRef, intRef, ushortRef, shortRef, sbyteRef, byteRef]) },
            { longRef, new RuntimeImplicitConversionRule(longRef, [uintRef, intRef, ushortRef, shortRef, sbyteRef, byteRef]) },
            { intRef, new RuntimeImplicitConversionRule(intRef, [ushortRef, shortRef, sbyteRef, byteRef]) },
            { uintRef, new RuntimeImplicitConversionRule(uintRef, [ushortRef, shortRef, sbyteRef, byteRef]) },
            { shortRef, new RuntimeImplicitConversionRule(shortRef, [sbyteRef, byteRef]) },
            { ushortRef, new RuntimeImplicitConversionRule(ushortRef, [sbyteRef, byteRef]) },
            { anyRef, new AnyConversionRule(anyRef, voidRef)},
            { arrayRef, new UniversalArrayRule(arrayRef) }
        };
    }
    
    private Dictionary<ICompileTimeFunction, FunctionDefinitionInfo> InitFuncs()
    {
        var funcInfoList = new List<KeyValuePair<ICompileTimeFunction, FunctionDefinitionInfo>>
        {
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(int)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(uint)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(long)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(ulong)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(float)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(double)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(string)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(char)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(bool)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Print), [typeof(object)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(int)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(uint)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(long)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(ulong)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(float)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(double)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(string)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(char)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(bool)])!),
            DescribeFunction(typeof(PrintIntrinsics).GetMethod(nameof(PrintIntrinsics.Println), [typeof(object)])!),
            DescribeFunction(typeof(LengthIntrinsics).GetMethod(nameof(LengthIntrinsics.Length), [typeof(Array)])!),
            DescribeFunction(typeof(LengthIntrinsics).GetMethod(nameof(LengthIntrinsics.Length), [typeof(string)])!),
            DescribeFunction(typeof(ReadIntrinsics).GetMethod(nameof(ReadIntrinsics.Read), [])!),
            DescribeFunction(typeof(ReadIntrinsics).GetMethod(nameof(ReadIntrinsics.Readln), [])!),
        };
        return funcInfoList.ToDictionary(x => x.Key, y => y.Value);
    }

    private KeyValuePair<ICompileTimeFunction, FunctionDefinitionInfo> DescribeFunction(MethodInfo info)
    {
        var returnType = info.ReturnType;
        var args = info.GetParameters();
        var name = info.Name;
        if (!TryGetFromClrType(returnType, out var returnTypeRef)) throw new Exception();

        var arguments = new List<KeyValuePair<string, ICompileTimeType>>();
        foreach (var parameter in args)
        {
            if (!TryGetFromClrType(parameter.ParameterType, out var argTypeRef)) throw new Exception();
            arguments.Add(new (parameter.Name!, argTypeRef));
        }

        var funcInfo = new FunctionDefinitionInfo()
        {
            ArgumentList = arguments,
            DefinitionPosition = default,
            Name = name,
            ReturnType = returnTypeRef
        };
        funcInfo.SetClrMethod(info);

        var fnRef = new RuntimeFunction(this, name, arguments.Select(x => x.Value).ToList());
        return new (fnRef, funcInfo);
    }
    
    private class AnyConversionRule(ICompileTimeType anyType, ICompileTimeType voidType) : IImplicitConversionRule
    {
        public ICompileTimeType ConversionTargetType { get; } = anyType;
        
        public int GetConversionCost(ICompileTimeType type)
        {
            return voidType.Equals(type) ? -1 : 1000;
        }
    }

    private class UniversalArrayRule(ICompileTimeType arrayType) : IImplicitConversionRule
    {
        public ICompileTimeType ConversionTargetType => arrayType;
        
        public int GetConversionCost(ICompileTimeType type)
        {
            return type.GetDefinitionInfo().ArrayUnderlyingType != null ? 1 : -1;
        }
    }

    private class RuntimeImplicitConversionRule(RuntimeType to, List<RuntimeType> sources) : IImplicitConversionRule
    {
        public ICompileTimeType ConversionTargetType { get; } = to;

        public int GetConversionCost(ICompileTimeType type)
        {
            if (type is not RuntimeType runtimeType) return -1;
            if (ConversionTargetType.Equals(runtimeType)) return 0;
            var index = sources.IndexOf(runtimeType);
            return index == -1 ? -1 : index + 1;
        }
    }

    private class RuntimeType(RuntimeSymbols table, string typeName, int arrayDefCount = 0) : ICompileTimeType
    {
        public string TypeName { get; } = typeName;
        public int ArrayDefCount { get; } = arrayDefCount;

        public ISymbolTable DeclaringTable => table;

        public bool Equals(ICompileTimeType? other)
        {
            return other is RuntimeType rt 
                   && rt.ArrayDefCount == ArrayDefCount
                   && rt.DeclaringTable == table 
                   && TypeName == other.TypeName;
        }
        
        public TypeDefinitionInfo GetDefinitionInfo()
        {
            return table._types[new TypeKey(TypeName, ArrayDefCount)];
        }

        public ICompileTimeType MakeArrayType()
        {
            var arrayRef = new RuntimeType(table, TypeName, ArrayDefCount + 1);
            var arrayKey = new TypeKey(TypeName, ArrayDefCount + 1);
            if (table._types.TryGetValue(arrayKey, out _)) return arrayRef;
            var currentInfo = GetDefinitionInfo();
            var info = new TypeDefinitionInfo()
            {
                ArrayUnderlyingType = this,
                DefinitionPosition = default,
                Fields = [],
                TypeName = TypeName
            };
            var arrayType = currentInfo.ClrType!.MakeArrayType();
            info.SetClrType(arrayType);
            table._types.Add(arrayKey, info);
            return arrayRef;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TypeName, table);
        }

        public override bool Equals(object? obj) => obj is RuntimeType type && Equals(type);

        [DoesNotReturn]
        public ICompileTimeField DefineField(string name, ICompileTimeType type) 
            => throw new InvalidOperationException("Cannot add field to compiler internal type");
    }
    
    private class RuntimeFunction(RuntimeSymbols symbols, string name, IReadOnlyList<ICompileTimeType> argumentTypes)
        : ICompileTimeFunction
    {
        public ISymbolTable DeclaringTable => symbols;

        public string Name { get; } = name;

        public IReadOnlyList<ICompileTimeType> ArgumentTypes { get; } = argumentTypes;

        public bool Equals(ICompileTimeFunction? other)
        {
            return other is RuntimeFunction fn
                   && symbols == fn.DeclaringTable
                   && Name == fn.Name
                   && StructuralComparisons.StructuralEqualityComparer.Equals(ArgumentTypes, fn.ArgumentTypes);
        }

        public FunctionDefinitionInfo GetDefinitionInfo() => symbols._funcs[this];

        public override bool Equals(object? obj) => obj is ICompileTimeFunction fn && Equals(fn);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var arg in ArgumentTypes)
            {
                hash.Add(arg);
            }

            return HashCode.Combine(hash.ToHashCode(), Name, DeclaringTable);
        }
    }

    private record struct TypeKey(string Name, int ArrayDefCount);
}