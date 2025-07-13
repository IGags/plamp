using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;

namespace plamp.Alternative.Tokenization.Token;

public class OperatorToken : TokenBase
{
    public OperatorEnum Operator { get; }
    
    public OperatorToken(string stringRepresentation, FilePosition start, FilePosition end, OperatorEnum @operator) 
        : base(start, end, stringRepresentation)
    {
        Operator = @operator;
    }
}