using System.Reflection.Emit;

namespace plamp.Abstractions.CompilerEmission;

/// <summary>
/// Represent single emission operation
/// </summary>
public interface IEmissionActor
{
    void Act(ILGenerator generator);
}