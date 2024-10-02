using System;

namespace plamp.Native;

public class InvalidGenericTypeException : Exception
{
    public InvalidGenericTypeException(string message) : base(message)
    {
        
    }
}