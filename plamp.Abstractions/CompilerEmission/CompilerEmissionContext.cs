using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.CompilerEmission;

public record CompilerEmissionContext(
    BodyNode MethodBody,
    MethodBuilder MethodBuilder,
    ParameterInfo[] Parameters, 
    ISymbolTable? SymbolTable);