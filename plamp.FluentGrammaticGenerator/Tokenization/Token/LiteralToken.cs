using System;

namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

public sealed record LiteralToken(string StringRepresentation, TokenPosition Start, TokenPosition End, Type LiteralType)
    : CommonTokenBase(StringRepresentation, Start, End)
{
    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), LiteralType);
    }

    public bool Equals(LiteralToken other)
    {
        return base.Equals(other) && other != null && LiteralType == other.LiteralType;
    }
}