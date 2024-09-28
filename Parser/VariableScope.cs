using System;
using System.Collections.Generic;
using System.Linq;
using Parser.Ast;

namespace Parser;

public class VariableScope : IDisposable
{
    private readonly VariableScope _parent;

    private readonly List<VariableDefinition> _variablesInScope = new();

    public int Depth { get; private set; }
    
    public VariableScope(VariableScope parent)
    {
        _parent = parent;
        Depth = parent?.Depth == null ? 0 : parent.Depth + 1;
    }

    public bool TryGetVariable(string name, out VariableDefinition variable)
    {
        var variableDefinition = _variablesInScope.FirstOrDefault(x => x.Name == name);
        if (variableDefinition != null)
        {
            variable = variableDefinition;
            return true;
        }

        if (_parent != null)
        {
            return _parent.TryGetVariable(name, out variable);
        }

        variable = null;
        return false;
    }

    public VariableDefinition GetVariable(string name)
    {
        if (TryGetVariable(name, out var variable))
        {
            return variable;
        }

        throw new ParserException($"variable with name {name} is not found");
    }
    
    public void AddVariable(VariableDefinition variable)
    {
        if (TryGetVariable(variable.Name, out _))
        {
            throw new ParserException($"variable with name {variable.Name} was defined above or in a parent scope");
        }

        _variablesInScope.Add(variable);
    }
    
    public VariableScope Enter()
    {
        return new VariableScope(this);
    }

    //Нужна, чтобы заворачивать в юзинг
    public void Dispose() {}
}