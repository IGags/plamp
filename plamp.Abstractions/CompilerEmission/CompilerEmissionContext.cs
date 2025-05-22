using System.Reflection.Emit;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.CompilerEmission;

public record CompilerEmissionContext(
    BodyNode MethodBody,
    MethodBuilder MethodBuilder,
    ICompiledAssemblyContainer CompiledAssemblyContainer, 
    ISymbolTable SymbolTable);