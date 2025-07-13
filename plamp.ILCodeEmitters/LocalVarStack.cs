using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace plamp.ILCodeEmitters;

internal class LocalVarStack
{
    private readonly Stack<List<KeyValuePair<string, LocalBuilder>>> _localVars = new();

    private readonly Dictionary<string, LocalBuilder> _vars = [];

    private List<KeyValuePair<string, LocalBuilder>> _currentScope = [];

    public LocalVarStack()
    {
        _localVars.Push([]);
    }
    
    public void BeginScope()
    {
        _currentScope = [];
        _localVars.Push(_currentScope);
    }

    public void EndScope()
    {
        Debug.Assert(_localVars.Count > 1);
        var values = _localVars.Pop();
        foreach (var varName in values)
        {
            _vars.Remove(varName.Key);
        }

        _currentScope = _localVars.Peek();
    }
    
    public bool Contains(string name) => _vars.ContainsKey(name);

    public void Add(string name, LocalBuilder type)
    {
        _vars[name] = type;
        _currentScope.Add(new (name, type));
    }
    
    public bool TryGetValue(string name, [MaybeNullWhen(false)]out LocalBuilder builder) => _vars.TryGetValue(name, out builder);
}