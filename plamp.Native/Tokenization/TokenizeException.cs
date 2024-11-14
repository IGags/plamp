using System;

namespace plamp.Native.Tokenization;

public class TokenizeException : Exception
{
    public TokenPosition StartPosition { get; }
    public TokenPosition EndPosition { get; }

    public TokenizeException(string message, TokenPosition startPosition, TokenPosition endPosition) : base(message)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
    }
}