using System;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public class ParserException : Exception
{
    public int EndPosition { get; }
    public int StartPosition { get; }

    public ParserException(string expected, TokenPosition startPosition, TokenPosition endPosition) 
        : base($"Unhandled parsing exception expected {expected}")
    {
        EndPosition = endPosition.Pos;
        StartPosition = startPosition.Pos;
    }

}