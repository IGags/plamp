namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

/// <summary>
/// A common token, lol
/// </summary>
public record CommonTokenBase(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : TokenBase(StringRepresentation, Start, End);