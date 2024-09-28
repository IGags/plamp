using System.Linq;

namespace Parser.Ast;

public record VariableDefinition
{
    private readonly TypeDescription _type;
    public string Name { get; }

    public VariableDefinition(TypeDescription type, string name)
    {
        if (name.ToKeyword() != Keywords.Unknown || !char.IsLetter(name.First()))
        {
            throw new VariableException(name);
        }
        _type = type;
        Name = name;
    }
}