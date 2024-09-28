using System;
using Parser.Token;

namespace Parser;

public class ParserException : Exception
{
    public ParserException(TokenBase tokenBase, string expected) 
        : base($"Unhandled parsing exception expected {expected}, but was {tokenBase.GetType().Name} with value {tokenBase.GetString()}") {}

    public ParserException(string message) : base(message)
    {
    }
}