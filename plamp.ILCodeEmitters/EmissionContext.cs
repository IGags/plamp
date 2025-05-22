using System.Reflection;
using System.Reflection.Emit;

namespace plamp.ILCodeEmitters;

internal record EmissionContext(
    LocalVarStack LocalVarStack,
    ParameterInfo[] Arguments,
    ILGenerator Generator,
    Dictionary<string, Label> Labels)
{
    private int _labelCounter;
    
    private readonly Stack<CycleContext> _currentCycles = [];

    public string NextLabel() => "lab_" + _labelCounter++;

    public void EnterCycleContext(string startLabel, string endLabel) 
        => _currentCycles.Push(new(startLabel, endLabel));        
    
    public void ExitCycleContext() => _currentCycles.Pop();
    
    public CycleContext? GetCurrentCycleContext() => _currentCycles.Count == 0 ? null : _currentCycles.Peek();
}