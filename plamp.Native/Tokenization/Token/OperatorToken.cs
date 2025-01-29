using plamp.Ast;
using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class OperatorToken : TokenBase
{
    public OperatorEnum Operator { get; }
    public OperatorToken(string stringRepresentation, FilePosition start, FilePosition end, OperatorEnum @operator) 
        : base(start, end, stringRepresentation)
    {
        Operator = @operator;
    }
}