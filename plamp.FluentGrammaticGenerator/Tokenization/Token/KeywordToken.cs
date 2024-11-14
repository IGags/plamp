namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public sealed record KeywordToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : CommonTokenBase(StringRepresentation, Start, End);