using System;

namespace plamp.Native;

public class TokenizeException : Exception
{
    public int Position { get; private set; }

    public TokenizeException(string message, int position) : base(message)
    {
        Position = position;
    }
}