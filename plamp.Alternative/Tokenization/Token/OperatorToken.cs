using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;

namespace plamp.Alternative.Tokenization.Token;

public class OperatorToken(string stringRepresentation, FilePosition position, OperatorEnum @operator) : TokenBase(position, stringRepresentation)
{
    public OperatorEnum Operator { get; } = @operator;
}