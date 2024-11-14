namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public sealed record PairToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : TokenBase(StringRepresentation, Start, End);