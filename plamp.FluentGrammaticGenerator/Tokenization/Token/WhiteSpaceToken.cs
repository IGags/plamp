namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public sealed record WhiteSpaceToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : TokenBase(StringRepresentation, Start, End)
{
    
}