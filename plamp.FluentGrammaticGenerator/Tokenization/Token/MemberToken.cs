namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public sealed record MemberToken(string StringRepresentation, TokenPosition Start, TokenPosition End) 
    : CommonTokenBase(StringRepresentation, Start, End);