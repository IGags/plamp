namespace plamp.Native.Parsing;

public class ParserErrorConstants
{
    public const string ExpectedTopLevel = "Expected top level statement like use or def";
    public const string ExpectedAssemblyName = "Expected assembly name";
    public const string ExpectedEndOfLine = "Expected end of line";
    public const string ExpectedUseStatement = "Expected use statement";
    public const string ExpectedDefStatement = "Expected def statement";
    public const string ExpectedParameter = "Expected a valid parameter defenition";
    public const string ExpectedWordPartTypeName = "Expected word (part of a type name)";
    public const string ExpectedInnerGenerics = "Empty generic definition";
    public const string ExpectedBodyLevelKeyword = "Expected body-level keyword (like: return, continue, break, for, if, while)";
    public const string ExpectedIfKeyword = "Expected if keyword";
    public const string ExpectedElifClause = "Expected a valid elif clause";
    public const string ExpectedElseClause = "Expected a valid else clause";
    public const string InvalidConditionBlock = "Invalid condition block";
    public const string ExpectedConditionExpression = "Expected condition expression";
    public const string ExpectedForKeyword = "Expected for keyword";
    public const string InvalidForHeaderDefinition = "Invalid for header";
    public const string ExpectedInKeyword = "Expected in keyword";
    public const string ExpectedWhileKeyword = "Expected while keyword";
    public const string ExpectedVariableDefinition = "Expected variable definition";
    public const string InvalidExpression = "Invalid expression";
    public const string EmptyIndexerDefinition = "Empty indexer definition";
    public const string InvalidOperator = "Invalid operator";
    public const string CannotUseKeyword = "Cannot use keyword here";
    public const string UnexpectedTokenPrefix = "Unexpected token";
}