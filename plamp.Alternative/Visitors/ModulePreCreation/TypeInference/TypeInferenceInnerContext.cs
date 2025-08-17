using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

public class TypeInferenceInnerContext(BaseVisitorContext other) : PreCreationContext(other)
{
    private int _monotonicScopeCounter;
    private int _currentDepth;
    
    private readonly Stack<ScopeLocation> _lexicalScopeStack = [];
    public Stack<Type?> InnerExpressionTypeStack { get; set; } = [];

    public Stack<int> StackSizeBeforeCall { get; set; } = [];
    
    public FuncNode? CurrentFunc { get; set; }

    public ScopeLocation InstructionInScopePosition { get; private set; } = new(-1, -1, -1);

    public Dictionary<string, VariableWithPosition> VariableDefinitions { get; } = [];
    public Dictionary<string, ParameterNode> Arguments { get; } = [];

    public void AddVariableWithPosition(VariableDefinitionNode variable, ScopeLocation position) 
        => VariableDefinitions[variable.Member.MemberName] = new VariableWithPosition(variable, position);

    public void EnterBody()
    {
        InnerExpressionTypeStack = [];
        _currentDepth++;
        if (InstructionInScopePosition.ScopeNumber != -1)
        {
            _lexicalScopeStack.Push(InstructionInScopePosition);
        }
        InstructionInScopePosition = new ScopeLocation(_currentDepth, 0, NextScopeNumber());
    }

    public void ExitBody()
    {
        _currentDepth--;
        InstructionInScopePosition = _lexicalScopeStack.TryPop(out var result) ? result : new (-1, -1, -1);
    }

    public void NextInstruction()
    {
        InnerExpressionTypeStack = [];
        if (InstructionInScopePosition is not {ScopeNumber: -1})
        {
            InstructionInScopePosition = InstructionInScopePosition with
            {
                PositionInScope = InstructionInScopePosition.PositionInScope + 1
            };
        }
    }

    private int NextScopeNumber() => _monotonicScopeCounter++;
}

/// <summary>
/// Represents position in lexical scope
/// </summary>
/// <param name="Depth">Current lexical scope depth</param>
/// <param name="PositionInScope">Instruction number in current scope</param>
/// <param name="ScopeNumber">Unique scope number</param>
public record struct ScopeLocation(int Depth, int PositionInScope, int ScopeNumber);