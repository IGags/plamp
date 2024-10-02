using System;

namespace plamp.Native;

public class VariableException : Exception
{
    public VariableException(string variableName) : base($"variable name {variableName} is invalid")
    {
        
    }
}