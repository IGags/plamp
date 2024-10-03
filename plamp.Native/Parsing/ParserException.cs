using System;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public class ParserException : Exception
{
    public int EndPosition { get; }
    public int StartPosition { get; }

    public ParserException(string expected, int startPosition, int endPosition) 
        : base($"Unhandled parsing exception expected {expected}")
    {
        EndPosition = endPosition;
        StartPosition = startPosition;
    }

}