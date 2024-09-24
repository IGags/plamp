using System.Collections.Generic;

namespace Parser.Ast;

public record FunctionDefinition(string Name, TypeDescription ReturnType, List<ParameterDescription> ParameterList);