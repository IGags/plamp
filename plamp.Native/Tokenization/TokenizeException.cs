using System;

namespace plamp.Native.Tokenization;

public class TokenizeException : Exception
{
    public int StartPosition { get; }
    public int EndPosition { get; }

    public TokenizeException(string message, int startPosition, int endPosition) : base(message)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
    }
}