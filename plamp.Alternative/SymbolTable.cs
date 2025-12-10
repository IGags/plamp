using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Intrinsics;

namespace plamp.Alternative;

/// <inheritdoc cref="ISymbolTable"/>
public class SymbolTable(string moduleName, List<ISymbolTable> dependencies) : ISymbolTable
{
    private readonly Dictionary<TypeKey, TypeDefinitionInfo> _definedTypes = new ();
    private readonly Dictionary<string, HashSet<FunctionDefinitionInfo>> _definedFuncs = [];
    
    /// <inheritdoc/>
    public string ModuleName { get; private set; } = moduleName;

    /// <summary>
    /// Установить имя текущего модуля.
    /// </summary>
    /// <param name="moduleName">Новое имя модуля.</param>
    public void SetModuleName(string moduleName) => ModuleName = moduleName;

    /// <summary>
    /// Создание типа-массива по типу из текущего модуля.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private CompileTimeType MakeArrayFromRef(CompileTimeType type)
    {
        var typeKey = new TypeKey(type.TypeName, type.ArrayDefCount);
        var info = _definedTypes[typeKey];

        var arrayTypeKey = new TypeKey(type.TypeName, type.ArrayDefCount + 1);
        if (_definedTypes.TryGetValue(arrayTypeKey, out _)) return arrayTypeKey.ToRef(this);
        
        var arrayInfo = new TypeDefinitionInfo()
        {
            Fields = [],
            ArrayUnderlyingType = type,
            DefinitionPosition = info.DefinitionPosition,
            TypeName = type.TypeName,
        };
        
        if (info.ClrType != null)
        {
            var arrayClr = info.ClrType.MakeArrayType();
            arrayInfo.SetClrType(arrayClr);
        }
        var arrayRef = arrayTypeKey.ToRef(this);

        _definedTypes[arrayTypeKey] = arrayInfo;
        return arrayRef;
    }
    
    /// <summary>
    /// Добавление поля к типу.
    /// </summary>
    /// <param name="name">Имя поля</param>
    /// <param name="fieldType">Тип поля</param>
    /// <param name="type">Тип объекта, содержащего поле</param>
    /// <returns>Было ли поле добавлено</returns>
    private ICompileTimeField? DefineField(CompileTimeType type, string name, ICompileTimeType fieldType)
    {
        var key = new TypeKey(type.TypeName, type.ArrayDefCount);
        var typeInfo = _definedTypes[key];
        var fld = typeInfo.Fields.FirstOrDefault(x => x.Name == name);
        if (fld != null && fld.Type.GetDefinitionInfo() != type.GetDefinitionInfo()) return null;
        if (fld != null) return new CompileTimeField(this, type, name);
        var info = new FieldDefinitionInfo() { Name = name, Type = fieldType };
        typeInfo.Fields.Add(info);
        var fldRef = new CompileTimeField(this, type, name);
        return fldRef;
    }

    /// <summary>
    /// Добавление объявления типа в таблицу. Если тип уже объявлен, то вернёт null.
    /// </summary>
    /// <param name="typeName">Имя типа</param>
    /// <param name="typeDefPosition">Позиция в файле, где был объявлен тип</param>
    public ICompileTimeType? TryAddType(string typeName, FilePosition typeDefPosition)
    {
        var def = new TypeDefinitionInfo()
        {
            Fields = [],
            TypeName = typeName,
            DefinitionPosition = typeDefPosition
        };
        var key = new TypeKey(typeName, 0);
        return !_definedTypes.TryAdd(key, def) ? null : key.ToRef(this);
    }
    
    /// <summary>
    /// Добавление объявления функции в таблицу. Если функция уже объявлена, то вернёт false,
    /// а в поле <see cref="fnRef"/> будет ссылка на старую перегрузку.
    /// </summary>
    /// <param name="name">Имя функции.</param>
    /// <param name="returnType">Возвращаемый тип</param>
    /// <param name="argumentTypes">Типы аргументов</param>
    /// <param name="definitionPosition">Позиция объявления в файле</param>
    /// <param name="fnRef">Ссылка на информацию о функции</param>
    public bool TryAddFunc(
        string name, 
        ICompileTimeType returnType, 
        List<ICompileTimeType> argumentTypes, 
        FilePosition definitionPosition,
        out ICompileTimeFunction fnRef)
    {
        var func = new FunctionDefinitionInfo()
        {
            ArgumentList = argumentTypes,
            DefinitionPosition = definitionPosition,
            ReturnType = returnType,
            Name = name
        };
        fnRef = new CompileTimeFunction(this, func.Name, func.ArgumentList);
        if (_definedFuncs.TryGetValue(func.Name, out var overloads))
        {
            var added = overloads.Add(func);
            return added;
        }
        overloads = [];
        _definedFuncs[func.Name] = overloads;
        return overloads.Add(func);
    }

    /// <inheritdoc/>
    public bool TryGetTypeByName(
        string typeName, 
        List<ArrayTypeSpecificationNode> arrayDefs, 
        [NotNullWhen(true)] out ICompileTimeType? type)
    {
        var key = new TypeKey(typeName, arrayDefs.Count);
        type = null;
        
        if (_definedTypes.TryGetValue(key, out _))
        {
            type = key.ToRef(this);
            return true;
        }

        if (arrayDefs.Count == 0) return false;
        
        key = new TypeKey(typeName, 0);
        if (!_definedTypes.TryGetValue(key, out _)) return false;
        type = key.ToRef(this);
        for (var i = 0; i < arrayDefs.Count; i++)
        {
            type = type.MakeArrayType();
        }

        return true;
    }

    public ICompileTimeFunction? GetMatchingFunction(string fnName, IReadOnlyList<ICompileTimeType?> signature)
    {
        if (!_definedFuncs.TryGetValue(fnName, out var overloads)) return null;
        var cost = int.MaxValue;
        FunctionDefinitionInfo? fn = null;
        foreach (var overload in overloads)
        {
            var currentCost = RuntimeSymbols.SymbolTable.MatchSignature(overload.ArgumentList, signature);
            if(currentCost < 0) continue;
            if (currentCost >= cost) continue;
            cost = currentCost;
            fn = overload;
        }

        if (fn == null) return null;
        return new CompileTimeFunction(this, fnName, fn.ArgumentList);
    }

    public IReadOnlyList<ICompileTimeFunction> ListFunctions() 
        => _definedFuncs.Values
            .SelectMany(x => x)
            .Select(x => new CompileTimeFunction(this, x.Name, x.ArgumentList))
            .ToList();

    public IReadOnlyList<ICompileTimeType> ListTypes() 
        => _definedTypes.Values.Select(x => new CompileTimeType(this, x.TypeName)).ToList();

    /// <inheritdoc/>
    public IReadOnlyList<ISymbolTable> GetDependencies() => dependencies;
    
    /// <inheritdoc/>
    private class CompileTimeType : ICompileTimeType
    {
        private readonly SymbolTable _symbolTable;

        public int ArrayDefCount { get; }
        
        /// <inheritdoc/>
        public string TypeName { get; }

        /// <inheritdoc/>
        public ISymbolTable DeclaringTable => _symbolTable;

        public CompileTimeType(SymbolTable symbolTable, string typeName, int arrayDefCount = 0)
        {
            _symbolTable = symbolTable;
            TypeName = typeName;
            _symbolTable = symbolTable;
            ArrayDefCount = arrayDefCount;
        }

        /// <inheritdoc/>
        public TypeDefinitionInfo GetDefinitionInfo() => _symbolTable._definedTypes[new TypeKey(TypeName, ArrayDefCount)];

        /// <inheritdoc/>
        public ICompileTimeType MakeArrayType() => _symbolTable.MakeArrayFromRef(this);

        /// <inheritdoc/>
        public ICompileTimeField? DefineField(string name, ICompileTimeType type) =>
            _symbolTable.DefineField(this, name, type);

        public bool Equals(ICompileTimeType? other)
        {
            return other is CompileTimeType ct 
                   && ct.DeclaringTable == DeclaringTable 
                   && TypeName == other.TypeName 
                   && ArrayDefCount == ct.ArrayDefCount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_symbolTable, TypeName, ArrayDefCount);
        }

        public override bool Equals(object? obj)
        {
            return obj is CompileTimeType typ && Equals(typ);
        }
    }

    /// <inheritdoc/>
    private class CompileTimeField : ICompileTimeField
    {
        private readonly SymbolTable _containingTable;
        private readonly CompileTimeType _containingType;

        /// <inheritdoc/>
        public ICompileTimeType ContainingType => _containingType;
        /// <inheritdoc/>
        public string Name { get; }

        /// 
        public CompileTimeField(
            SymbolTable containingTable,
            CompileTimeType containingType, 
            string name)
        {
            _containingTable = containingTable;
            _containingType = containingType;
            Name = name;
        }

        /// <inheritdoc/>
        public FieldDefinitionInfo GetDefinitionInfo()
        {
            var typeInfo =
                _containingTable._definedTypes[new TypeKey(_containingType.TypeName, _containingType.ArrayDefCount)];
            return typeInfo.Fields.First(x => x.Name == Name);
        }
    }
    
    /// <inheritdoc/>
    private class CompileTimeFunction : ICompileTimeFunction
    {
        private readonly SymbolTable _symbolTable;

        /// <inheritdoc/>
        public ISymbolTable DeclaringTable => _symbolTable;
        
        /// <inheritdoc/>
        public string Name { get; }
        
        /// <inheritdoc/>
        public IReadOnlyList<ICompileTimeType> ArgumentTypes { get; }

        /// 
        public CompileTimeFunction(SymbolTable declaringTable, string name, IReadOnlyList<ICompileTimeType> argumentTypes)
        {
            _symbolTable = declaringTable;
            Name = name;
            ArgumentTypes = argumentTypes;
        }

        /// <inheritdoc/>
        public FunctionDefinitionInfo GetDefinitionInfo()
        {
            var overloads = _symbolTable._definedFuncs[Name];
            return overloads.First(x =>
                StructuralComparisons.StructuralEqualityComparer.Equals(x.ArgumentList, ArgumentTypes));
        }

        public bool Equals(ICompileTimeFunction? other)
        {
            if (other == null) return false;
            return DeclaringTable.Equals(other.DeclaringTable) 
                   && Name == other.Name 
                   && StructuralComparisons.StructuralEqualityComparer.Equals(ArgumentTypes, other.ArgumentTypes);
        }

        public override bool Equals(object? obj)
        {
            return obj is CompileTimeFunction fn && Equals(fn);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var type in ArgumentTypes)
            {
                hash.Add(type);
            }
            return HashCode.Combine(DeclaringTable, Name, hash.ToHashCode());
        }
    }

    private record struct TypeKey(string Name, int ArrayDefCount)
    {
        public CompileTimeType ToRef(SymbolTable symbolTable) => new CompileTimeType(symbolTable, Name, ArrayDefCount);
    }
}