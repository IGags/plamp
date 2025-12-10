using System.Reflection.Emit;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class CreationContext : BaseVisitorContext
{
    public AssemblyBuilder AssemblyBuilder { get; }

    public ModuleBuilder ModuleBuilder { get; }

    public SymbolTable SymbolTable { get; init; }

    public CreationContext(
        AssemblyBuilder assemblyBuilder, 
        ModuleBuilder moduleBuilder, 
        SymbolTable symbolTable, 
        BaseVisitorContext other) : base(other)
    {
        AssemblyBuilder = assemblyBuilder;
        ModuleBuilder = moduleBuilder;
        TranslationTable = other.TranslationTable;
        Exceptions = other.Exceptions;
        Dependencies = other.Dependencies;
        SymbolTable = symbolTable;
    }

    public CreationContext(CreationContext other) : base(other)
    {
        AssemblyBuilder = other.AssemblyBuilder;
        ModuleBuilder = other.ModuleBuilder;
        SymbolTable = other.SymbolTable;
    }
}