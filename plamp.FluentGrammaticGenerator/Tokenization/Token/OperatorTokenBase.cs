namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public abstract record OperatorTokenBase(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : CommonTokenBase(StringRepresentation, Start, End);