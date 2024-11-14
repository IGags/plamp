namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public record MultiLineCommentToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : TokenBase(StringRepresentation, Start, End);