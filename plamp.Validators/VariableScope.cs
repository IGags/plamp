using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Validators;

public class VariableScope : IDisposable
{
    private readonly VariableScope _parent;

    private readonly List<BaseVariableExpression> _variablesInScope = new();

    public int Depth { get; private set; }
    
    public VariableScope(VariableScope parent)
    {
        _parent = parent;
        Depth = parent?.Depth == null ? 0 : parent.Depth + 1;
    }

    public bool TryGetVariable(string name, out BaseVariableExpression variable)
    {
        var variableDefinition = _variablesInScope.FirstOrDefault(x => x.VariableDefinition.Name == name);
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

    public BaseVariableExpression GetVariable(string name)
    {
        if (TryGetVariable(name, out var variable))
        {
            return variable;
        }

        throw new ParserException($"variable with name {name} is not found");
    }
    
    public void AddVariable(BaseVariableExpression variable)
    {
        if (TryGetVariable(variable.VariableDefinition.Name, out _))
        {
            throw new ParserException($"variable with name {variable.VariableDefinition.Name} was defined above or in a parent scope");
        }

        _variablesInScope.Add(variable);
    }
    
    public VariableScope Enter()
    {
        return new VariableScope(this);
    }

    //Нужна, чтобы заворачивать в юзинг
    public void Dispose() {}

    public IReadOnlyList<BaseVariableExpression> GetAllVariables(List<BaseVariableExpression> varList = null)
    {
        if (varList == null)
        {
            varList = new List<BaseVariableExpression>();
            varList.AddRange(_variablesInScope);
        }
        else
        {
            varList.AddRange(_variablesInScope.Select(x => new VariableExpression(x.VariableDefinition)));
        }
        return _parent == null ? varList : _parent.GetAllVariables(varList);
    }
}