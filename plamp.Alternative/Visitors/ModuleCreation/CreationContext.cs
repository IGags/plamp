using System.Reflection.Emit;
using plamp.Abstractions.AstManipulation;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class CreationContext : BaseVisitorContext
{
    public AssemblyBuilder AssemblyBuilder { get; }

    public ModuleBuilder ModuleBuilder { get; }
    
    public ISymTableBuilder CurrentModuleBuilder { get; }

    public CreationContext(
        AssemblyBuilder assemblyBuilder, 
        ModuleBuilder moduleBuilder,
        ISymTableBuilder currentModuleBuilder,
        BaseVisitorContext other) : base(other)
    {
        AssemblyBuilder = assemblyBuilder;
        ModuleBuilder = moduleBuilder;
        CurrentModuleBuilder = currentModuleBuilder;
        TranslationTable = other.TranslationTable;
        Exceptions = other.Exceptions;
        Dependencies = other.Dependencies;
    }

    public CreationContext(CreationContext other) : base(other)
    {
        AssemblyBuilder = other.AssemblyBuilder;
        ModuleBuilder = other.ModuleBuilder;
        CurrentModuleBuilder = other.CurrentModuleBuilder;
    }
}