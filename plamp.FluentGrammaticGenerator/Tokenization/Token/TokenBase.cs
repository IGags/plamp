using System;

namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

/// <summary>
/// A base class for token definition
/// </summary>
public abstract record TokenBase(string StringRepresentation, TokenPosition Start, TokenPosition End)
{
    public override int GetHashCode() 
        => HashCode.Combine(StringRepresentation.GetHashCode(), Start.GetHashCode(), End.GetHashCode());

    public virtual bool Equals(TokenBase other)
    {
        if (other == null)
        {
            return false;
        }

        return StringRepresentation == other.StringRepresentation 
               && Start.Equals(other.Start) && End.Equals(other.End);
    }
}