using System;

namespace Parser;

public class InvalidGenericTypeException : Exception
{
    public InvalidGenericTypeException(string message) : base(message)
    {
        
    }
}