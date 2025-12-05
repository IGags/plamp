using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using plamp.Abstractions;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Intrinsics;

/// <inheritdoc cref="ISymbolTable"/>
public class RuntimeSymbols : ISymbolTable
{
    private readonly Dictionary<ICompileTimeType, TypeDefinitionInfo> _types;
    private readonly Dictionary<ICompileTimeType, IImplicitConversionRule> _conversionRules;
    private readonly Dictionary<ICompileTimeFunction, FunctionDefinitionInfo> _funcs;
    
    public static readonly RuntimeSymbols GetSymbolTable = new();

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
        => _types.TryGetValue(type, out var value) && value.ClrType == typeof(void);

    public bool IsAny(ICompileTimeType type)
        => _types.TryGetValue(type, out var value) && value.ClrType == typeof(object);

    public bool IsNumeric(ICompileTimeType type)
    {
        if (!_types.TryGetValue(type, out var typeDef)) return false;
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
        if (!_types.TryGetValue(type, out var value)) return false;
        return value.ClrType == typeof(bool);
    }

    /// <inheritdoc/>
    public bool TryGetTypeByName(string typeName, List<ArrayTypeSpecificationNode> arrayDefs, [NotNullWhen(true)] out ICompileTimeType? type)
    {
        type = null;
        var typeRef = _types.FirstOrDefault(x => x.Key.TypeName == typeName).Key;
        if (typeRef != null)
        {
            for (var i = 0; i < arrayDefs.Count; i++)
            {
                typeRef = typeRef.MakeArrayType();
            }
            type = typeRef;
            return true;
        }

        return false;
    }

    public ICompileTimeType MakeVoid() => new RuntimeType(this, VoidName);

    public ICompileTimeType MakeLogical() => new RuntimeType(this, BoolName);

    public ICompileTimeType MakeInt() => new RuntimeType(this, IntName);

    public ICompileTimeType MakeUint() => new RuntimeType(this, UintName);

    public ICompileTimeType MakeShort() => new RuntimeType(this, ShortName);

    public ICompileTimeType MakeUshort() => new RuntimeType(this, UshortName);

    public ICompileTimeType MakeByte() => new RuntimeType(this, ByteName);

    public ICompileTimeType MakeSbyte() => new RuntimeType(this, SbyteName);

    public ICompileTimeType MakeLong() => new RuntimeType(this, LongName);

    public ICompileTimeType MakeUlong() => new RuntimeType(this, UlongName);

    public ICompileTimeType MakeDouble() => new RuntimeType(this, DoubleName);

    public ICompileTimeType MakeFloat() => new RuntimeType(this, FloatName);

    public ICompileTimeType MakeAny() => new RuntimeType(this, AnyName);

    public ICompileTimeType MakeString() => new RuntimeType(this, StringName);

    public ICompileTimeType MakeChar() => new RuntimeType(this, CharName);

    public bool TryGetFromClrType(Type clrType, out ICompileTimeType type)
    {
        type = _types.FirstOrDefault(x => x.Value.ClrType == clrType).Key;
        return type != null;
    }
    
    /// <inheritdoc/>
    public bool TryGetFunction(
        string fnName, 
        IReadOnlyList<ICompileTimeType> signature, 
        [NotNullWhen(true)] out ICompileTimeFunction? function)
    {
        function = null;
        var matching = GetMatchingFunctions(fnName, signature);
        if (matching.Length != 1) return false;
        function = matching.First();
        return true;
    }
    
    /// <inheritdoc/>
    public ICompileTimeFunction[] GetMatchingFunctions(string fnName, IReadOnlyList<ICompileTimeType> signature)
    {
        var overloads = _funcs.Keys.Where(x => x.Name == fnName);
        var matching = overloads.Where(x => MatchSignature(x.ArgumentTypes, signature));
        return matching.ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISymbolTable> GetDependencies() => [];

    /// <summary>
    /// Метод, позволяющий проверить, что есть возможность неявной конверсии.
    /// Применяется при выводе типов. Возвращает true, если тип равен сам себе.
    /// </summary>
    /// <param name="from">Тип из которого надо проверить конверсию</param>
    /// <param name="to">Тип, в который надо проверить конверсию</param>
    /// <returns>Флаг возможности проведения конверсии</returns>
    public bool TypeIsImplicitlyConvertable(ICompileTimeType from, ICompileTimeType to) 
        => from.Equals(to) || _conversionRules.TryGetValue(to, out var rule) && rule.Convertable(from);

    /// <summary>
    /// Метод-помогатор, позволяет по правилам языка проверить, что сигнатура метода подходит с точностью до неявных преобразований типа.
    /// </summary>
    /// <param name="signatureTypes">Типы аргументов объявленной функции.</param>
    /// <param name="actualTypes">Типы аргументов, для проверки.</param>
    public bool MatchSignature(
        IReadOnlyList<ICompileTimeType> signatureTypes,
        IReadOnlyList<ICompileTimeType> actualTypes)
    {
        if (signatureTypes.Count != actualTypes.Count) return false;
        for (var i = 0; i < signatureTypes.Count; i++)
        {
            if (!TypeIsImplicitlyConvertable(actualTypes[i], signatureTypes[i])) return false;
        }

        return true;
    }

    private Dictionary<ICompileTimeType, TypeDefinitionInfo> InitTypes()
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
            {new RuntimeType(this, voidDef.TypeName),   voidDef},
            {new RuntimeType(this, intDef.TypeName),    intDef},
            {new RuntimeType(this, uintDef.TypeName),   uintDef},
            {new RuntimeType(this, longDef.TypeName),   longDef},
            {new RuntimeType(this, ulongDef.TypeName),  ulongDef},
            {new RuntimeType(this, charDef.TypeName),   charDef},
            {new RuntimeType(this, stringDef.TypeName), stringDef},
            {new RuntimeType(this, byteDef.TypeName),   byteDef},
            {new RuntimeType(this, boolDef.TypeName),   boolDef},
            {new RuntimeType(this, shortDef.TypeName),  shortDef},
            {new RuntimeType(this, ushortDef.TypeName), ushortDef},
            {new RuntimeType(this, sbyteDef.TypeName),  sbyteDef},
            {new RuntimeType(this, floatDef.TypeName),  floatDef},
            {new RuntimeType(this, doubleDef.TypeName), doubleDef},
            {new RuntimeType(this, anyDef.TypeName),    anyDef},
            {new RuntimeType(this, arrayDef.TypeName),  arrayDef}
        };
    }

    private Dictionary<ICompileTimeType, IImplicitConversionRule> InitConversionRules()
    {
        var byteRef = (RuntimeType)MakeByte();
        var sbyteRef = (RuntimeType)MakeSbyte();
        var shortRef = (RuntimeType)MakeShort();
        var ushortRef = (RuntimeType)MakeUshort();
        var intRef = (RuntimeType)MakeInt();
        var uintRef = (RuntimeType)MakeUint();
        var longRef = (RuntimeType)MakeLong();
        var ulongRef = (RuntimeType)MakeUlong();
        var floatRef = (RuntimeType)MakeFloat();
        var doubleRef = (RuntimeType)MakeDouble();
        var anyRef = MakeAny();
        var voidRef = MakeVoid();
        var arrayRef = new RuntimeType(this, ArrayName);

        return new()
        {
            { doubleRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef, shortRef, ushortRef, intRef, uintRef, longRef, ulongRef, floatRef]) },
            { floatRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef, shortRef, ushortRef, intRef, uintRef, longRef, ulongRef]) },
            { ulongRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef, shortRef, ushortRef, intRef, uintRef]) },
            { longRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef, shortRef, ushortRef, intRef, uintRef]) },
            { intRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef, shortRef, ushortRef]) },
            { uintRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef, shortRef, ushortRef]) },
            { shortRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef]) },
            { ushortRef, new RuntimeImplicitConversionRule(doubleRef, [byteRef, sbyteRef]) },
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

        var arguments = new List<ICompileTimeType>();
        foreach (var parameter in args)
        {
            if (!TryGetFromClrType(parameter.ParameterType, out var argTypeRef)) throw new Exception();
            arguments.Add(argTypeRef);
        }

        var funcInfo = new FunctionDefinitionInfo()
        {
            ArgumentList = arguments,
            DefinitionPosition = default,
            Name = name,
            ReturnType = returnTypeRef
        };
        funcInfo.SetClrMethod(info);

        var fnRef = new RuntimeFunction(this, name, arguments);
        return new (fnRef, funcInfo);
    }
    
    private class AnyConversionRule(ICompileTimeType anyType, ICompileTimeType voidType) : IImplicitConversionRule
    {
        public ICompileTimeType ApplicableForTargetType { get; } = anyType;

        public bool Convertable(ICompileTimeType type)
        {
            return !voidType.Equals(type);
        }
    }

    private class UniversalArrayRule(ICompileTimeType arrayType) : IImplicitConversionRule
    {
        public ICompileTimeType ApplicableForTargetType => arrayType;
        
        public bool Convertable(ICompileTimeType type) => type.GetDefinitionInfo().ArrayUnderlyingType != null;
    }

    private class RuntimeImplicitConversionRule(RuntimeType to, List<RuntimeType> sources) : IImplicitConversionRule
    {
        private readonly HashSet<RuntimeType> _sources = sources.ToHashSet();
        
        public ICompileTimeType ApplicableForTargetType { get; } = to;

        public bool Convertable(ICompileTimeType type)
        {
            if (type is not RuntimeType runtimeType) return false;
            if (ApplicableForTargetType.Equals(runtimeType)) return false;
            return _sources.Contains(type);
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
            return table._types[this];
        }

        public ICompileTimeType MakeArrayType()
        {
            var arrayRef = new RuntimeType(table, TypeName, ArrayDefCount + 1);
            if (table._types.TryGetValue(arrayRef, out _)) return arrayRef;
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
            table._types.Add(arrayRef, info);
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
}