using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.CompilerEmission;

public record CompilerEmissionContext(
    BodyNode MethodBody,
    MethodBuilder MethodBuilder,
    ParameterInfo[] Parameters,
    ICompiledAssemblyContainer CompiledAssemblyContainer, 
    ISymbolTable SymbolTable);