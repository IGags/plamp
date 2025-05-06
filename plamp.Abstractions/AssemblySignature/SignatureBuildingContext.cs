using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.CompilerEmission;

namespace plamp.Abstractions.AssemblySignature;

public record SignatureBuildingContext(
    IAssemblyBuilderCreator AssemblyBuilderCreator,
    ICompiledAssemblyContainer CompiledAssemblyContainer,
    ISymbolTable SymbolTable,
    AssemblyName AssemblyName,
    string ModuleName = null);