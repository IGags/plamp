namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

/// <summary>
/// A base token that defines end of statement
/// </summary>
public abstract record StatementSeparatorToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : CommonTokenBase(StringRepresentation, Start, End);