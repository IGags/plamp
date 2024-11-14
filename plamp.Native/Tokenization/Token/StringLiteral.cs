namespace plamp.Native.Tokenization.Token;

public class StringLiteral : TokenBase
{
    public StringLiteral(string value, TokenPosition start, TokenPosition end) : base(start, end, value) { }
}