namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public sealed record SingleLineCommentToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : TokenBase(StringRepresentation, Start, End);