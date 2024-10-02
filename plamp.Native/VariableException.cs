using System;

namespace Parser;

public class VariableException : Exception
{
    public VariableException(string variableName) : base($"variable name {variableName} is invalid")
    {
        
    }
}