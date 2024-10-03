using System;

namespace plamp.Validators;

public class UnexistingTypeException : Exception
{
    public UnexistingTypeException(string type) : base($"The {type} type does not exists")
    {
        
    }
}