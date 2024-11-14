using System;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public class ParserException : Exception
{
    public TokenPosition EndPosition { get; }
    public TokenPosition StartPosition { get; }

    public ParserException(string message, TokenPosition startPosition, TokenPosition endPosition) 
        : base(message)
    {
        EndPosition = endPosition;
        StartPosition = startPosition;
    }
}