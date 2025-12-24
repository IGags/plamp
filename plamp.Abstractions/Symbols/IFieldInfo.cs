using System.Reflection;

namespace plamp.Abstractions.Symbols;

public interface IFieldInfo
{
    public FieldInfo AsField();
    
    public ITypeInfo FieldType { get; }
    
    public string Name { get; }
}