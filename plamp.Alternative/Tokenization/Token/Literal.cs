using System;
using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class Literal(string stringValue, FilePosition position, object actualValue, Type actualType) : TokenBase(position, stringValue)
{
    public object ActualValue { get; } = actualValue;
    public Type ActualType { get; } = actualType;
}