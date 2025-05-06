using System.Reflection.Emit;
using plamp.Abstractions.CompilerEmission;

namespace plamp.ILCodeEmitters;

internal class SequentialActor : IEmissionActor
{
    private readonly List<Action<ILGenerator>> _instructions = [];

    public readonly Dictionary<string, LocalBuilder> Locals = [];

    public readonly Dictionary<string, Label> Labels = [];
    
    public void Act(ILGenerator generator)
    {
        foreach (var instruction in _instructions)
        {
            instruction(generator);
        }
    }

    public void DefineLabel(string labelName)
    {
        _instructions.Add(g =>
        {
            var label = g.DefineLabel();
            Labels.Add(labelName, label);
        });
    }

    public void DeclareLocal(string localName, Type localType)
    {
        _instructions.Add(g =>
        {
            var local = g.DeclareLocal(localType);
            Locals.Add(localName, local);
        });
    }

    public void Add(Action<ILGenerator> instruction) => _instructions.Add(instruction);
}