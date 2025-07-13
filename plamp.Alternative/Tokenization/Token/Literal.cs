using System;
using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class Literal : TokenBase
{
    public object ActualValue { get; }
    public Type ActualType { get; }

    public Literal(string stringValue, FilePosition start, FilePosition end, object actualValue, Type actualType) 
        : base(start, end, stringValue)
    {
        ActualValue = actualValue;
        ActualType = actualType;
    }
}