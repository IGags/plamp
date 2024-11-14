using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class OperatorToken : TokenBase
{
    public OperatorEnum Operator { get; }
    public OperatorToken(string stringRepresentation, TokenPosition start, TokenPosition end, OperatorEnum @operator) 
        : base(start, end, stringRepresentation)
    {
        Operator = @operator;
    }
}