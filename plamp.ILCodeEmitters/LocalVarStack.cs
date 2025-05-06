namespace plamp.ILCodeEmitters;

internal class LocalVarStack
{
    private readonly Stack<List<KeyValuePair<string, Type>>> _localVars = [];

    private readonly Dictionary<string, Type> _vars = [];

    private List<KeyValuePair<string, Type>> _currentScope = [];
    
    public void BeginScope()
    {
        _currentScope = [];
        _localVars.Push(_currentScope);
    }

    public void EndScope()
    {
        var values = _localVars.Pop();
        foreach (var varName in values)
        {
            _vars.Remove(varName.Key);
        }

        _currentScope = _localVars.Peek();
    }
    
    public bool Contains(string name) => _vars.ContainsKey(name);

    public void Add(string name, Type type)
    {
        _vars[name] = type;
        _currentScope.Add(new (name, type));
    }
    
    public bool TryGetValue(string name, out Type? type) => _vars.TryGetValue(name, out type);
}