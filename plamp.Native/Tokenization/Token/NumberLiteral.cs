using System;
using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class NumberLiteral : TokenBase
{
    public object ActualValue { get; }
    public Type ActualType { get; }

    public NumberLiteral(string stringValue, FilePosition start, FilePosition end, object actualValue, Type actualType) 
        : base(start, end, stringValue)
    {
        ActualValue = actualValue;
        ActualType = actualType;
    }
}