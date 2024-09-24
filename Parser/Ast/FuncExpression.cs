namespace Parser.Ast;

public record FuncExpression(string Name, TypeDescription ReturnType, ParameterDescription[] ParameterList, BodyExpression Body)
{ 
}