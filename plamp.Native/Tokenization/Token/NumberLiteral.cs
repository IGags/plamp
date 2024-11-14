using System;

namespace plamp.Native.Tokenization.Token;

public class NumberLiteral : TokenBase
{
    public object ActualValue { get; }
    public Type ActualType { get; }

    public NumberLiteral(string stringValue, TokenPosition start, TokenPosition end, object actualValue, Type actualType) 
        : base(start, end, stringValue)
    {
        ActualValue = actualValue;
        ActualType = actualType;
    }
}