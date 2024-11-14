using System;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization;

public class UnexpectedTokenException : Exception
{
    public TokenPosition Start { get; }
    public TokenPosition End { get; }

    public UnexpectedTokenException(TokenPosition start, TokenPosition end) : base("Unexpected token")
    {
        Start = start;
        End = end;
    }
}