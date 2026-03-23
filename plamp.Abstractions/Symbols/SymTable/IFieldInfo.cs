using System;
using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

public interface IFieldInfo : IEquatable<IFieldInfo>
{
    public FieldInfo AsField();
    
    public ITypeInfo FieldType { get; }
    
    public string Name { get; }
}