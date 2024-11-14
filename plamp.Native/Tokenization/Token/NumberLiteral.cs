namespace plamp.Native.Tokenization.Token;

public class NumberLiteral : TokenBase
{
    public NumberLiteral(string stringValue, TokenPosition start, TokenPosition end) : base(start, end, stringValue)
    {
    }
}