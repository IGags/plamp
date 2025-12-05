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
    private readonly Dictionary<CompileTimeType, TypeDefinitionInfo> _definedTypes = new ();
    private readonly Dictionary<CompileTimeFunction, FunctionDefinitionInfo> _definedFuncs = [];
    
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
        var info = _definedTypes[type];

        var arrayTypeKey = new CompileTimeType(this, type.TypeName);

        if (_definedTypes.TryGetValue(arrayTypeKey, out _)) return arrayTypeKey;
        
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
        var arrayRef = new CompileTimeType(this, arrayInfo.TypeName, type.ArrayDefCount + 1);

        _definedTypes[arrayRef] = arrayInfo;
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
        var typeInfo = _definedTypes[type];
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
        var key = new CompileTimeType(this, typeName);
        return !_definedTypes.TryAdd(key, def) ? null : key;
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
        var key = new CompileTimeFunction(this, func.Name, func.ArgumentList);
        var add = _definedFuncs.TryAdd(key, func);
        fnRef = key;
        return add;
    }

    /// <inheritdoc/>
    public bool TryGetTypeByName(string typeName, List<ArrayTypeSpecificationNode> arrayDefs, [NotNullWhen(true)] out ICompileTimeType? type)
    {
        type = null;
        var typeRef = _definedTypes.FirstOrDefault(x => x.Key.TypeName == typeName).Key;
        if (typeRef != null)
        {
            type = typeRef;
            for (var i = 0; i < arrayDefs.Count; i++)
            {
                type = type.MakeArrayType();
            }
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool TryGetFunction(string fnName, IReadOnlyList<ICompileTimeType> signature, [NotNullWhen(true)] out ICompileTimeFunction? function)
    {
        function = null;
        var matchingFuncs = GetMatchingFunctions(fnName, signature);
        if (matchingFuncs.Length != 1) return false;
        function = matchingFuncs[0];
        return true;
    }

    public ICompileTimeFunction[] GetMatchingFunctions(string fnName, IReadOnlyList<ICompileTimeType> signature)
    {
        var matchingFuncs = _definedFuncs.Keys
            .Where(x => x.Name == fnName && RuntimeSymbols.GetSymbolTable.MatchSignature(x.ArgumentTypes, signature))
            .Cast<ICompileTimeFunction>().ToArray();
        return matchingFuncs;
    }

    public IReadOnlyList<ICompileTimeFunction> ListFunctions() => _definedFuncs.Keys.ToList();

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
        public TypeDefinitionInfo GetDefinitionInfo() => _symbolTable._definedTypes[this];

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
            var typeInfo = _containingTable._definedTypes[_containingType];
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
            return _symbolTable._definedFuncs[this];
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
}