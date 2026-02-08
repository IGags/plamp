using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public class TypeInferenceInnerContext : PreCreationContext
{
    private readonly Stack<int> _typeInferenceSizeSnapshotStack = [];

    #region Variable scope validation handling

    private readonly Stack<List<VariableDefinitionNode>> _scopeDefinitionStack = [];

    private readonly Dictionary<string, VariableDefinitionNode> _variableDefinitions = [];

    #endregion
    
    public Stack<ITypeInfo?> InnerExpressionTypeStack { get; private set; } = [];

    public FuncNode? CurrentFunc { get; set; }

    public Dictionary<string, ParameterNode> Arguments { get; } = [];
    
    public TypeInferenceInnerContext(PreCreationContext other) : base(other)
    {
        //Защита от дурака и способ корректно работать с тестами, в которых нет body как такового
        _scopeDefinitionStack.Push([]);
    }

    public bool TryAddVariable(
        VariableDefinitionNode variable)
    {
        if (!_variableDefinitions.TryAdd(variable.Name.Value, variable)) return false;
        _scopeDefinitionStack.Peek().Add(variable);
        return true;
    }

    public bool TryGetVariable(string variableName, [NotNullWhen(true)]out VariableDefinitionNode? varInfo)
    {
        return _variableDefinitions.TryGetValue(variableName, out varInfo);
    }

    public void EnterBody()
    {
        InnerExpressionTypeStack = [];
        _scopeDefinitionStack.Push([]);
    }

    public void ExitBody()
    {
        var definedVariables = _scopeDefinitionStack.Pop();
        foreach (var variable in definedVariables)
        {
            _variableDefinitions.Remove(variable.Name.Value);
        }
    }

    public void NextInstruction()
    {
        InnerExpressionTypeStack = [];
    }

    public void SaveInferenceStackSize() => _typeInferenceSizeSnapshotStack.Push(InnerExpressionTypeStack.Count);

    public int RestoreInferenceStackSize() => _typeInferenceSizeSnapshotStack.Pop();
}