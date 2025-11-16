using System;
using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast;

namespace plamp.Abstractions;


public class TypeDefinitionInfo
{
    public string TypeName { get; }
    
    public string ModuleName { get; }
    
    public Type? ClrType { get; private set; }
    
    public FilePosition DefinitionPosition { get; }
    
    public List<FieldDefinitionInfo> Fields { get; }

    public void SetClrType(Type clrType) => ClrType = clrType;
}

public class FieldDefinitionInfo
{
    public string Name { get; }
    
    public TypeDefinitionInfo Type { get; }
    
    public FieldInfo? ClrField { get; private set; }

    public void SetClrField(FieldInfo clrField) => ClrField = clrField;
}

public class FunctionDefinitionInfo
{
    public string Name { get; }
    
    public TypeDefinitionInfo ReturnType { get; }
    
    public List<TypeDefinitionInfo> ArgumentList { get; }
    
    public FilePosition DefinitionPosition { get; }
    
    public MethodInfo? ClrMethod { get; set; }

    public void SetClrMethod(MethodInfo clrMethod) => ClrMethod = clrMethod;
}