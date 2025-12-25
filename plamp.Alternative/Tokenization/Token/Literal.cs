using plamp.Abstractions.Ast;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.Tokenization.Token;

public class Literal(string stringValue, FilePosition position, object actualValue, ITypeInfo actualType) : TokenBase(position, stringValue)
{
    public object ActualValue { get; } = actualValue;
    public ITypeInfo ActualType { get; } = actualType;
}