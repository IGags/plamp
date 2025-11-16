using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class CreationContext : BaseVisitorContext
{
    public AssemblyBuilder AssemblyBuilder { get; }

    public ModuleBuilder ModuleBuilder { get; }

    public List<MethodBuilder> Methods { get; }

    public CreationContext(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder, BaseVisitorContext other) : base(other)
    {
        AssemblyBuilder = assemblyBuilder;
        ModuleBuilder = moduleBuilder;
        Methods = [];
        TranslationTable = other.TranslationTable;
        Exceptions = other.Exceptions;
        ModuleName = other.ModuleName;
        Functions = other.Functions;
    }

    public CreationContext(CreationContext other) : base(other)
    {
        AssemblyBuilder = other.AssemblyBuilder;
        ModuleBuilder = other.ModuleBuilder;
        Methods = other.Methods;
    }
}