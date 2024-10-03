using System;

namespace plamp.Validators;

public class InvalidGenericTypeException : Exception
{
    public InvalidGenericTypeException(string message) : base(message)
    {
        
    }
}