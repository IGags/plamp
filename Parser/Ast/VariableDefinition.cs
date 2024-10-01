using System;
using System.Linq;

namespace Parser.Ast;

public record VariableDefinition
{
    public readonly Type Type;
    public string Name { get; }

    public VariableDefinition(Type type, string name)
    {
        if (name.ToKeyword() != Keywords.Unknown || !char.IsLetter(name.First()))
        {
            throw new VariableException(name);
        }
        Type = type;
        Name = name;
    }
}