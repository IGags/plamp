using System;
using plamp.Native.Token;

namespace plamp.Native;

public class ParserException : Exception
{
    public int EndPosition { get; }
    public int StartPosition { get; }

    public ParserException(TokenBase tokenBase, string expected, int startPosition, int endPosition) 
        : base($"Unhandled parsing exception expected {expected}, but was {tokenBase.GetString()}")
    {
        EndPosition = endPosition;
        StartPosition = startPosition;
    }

}