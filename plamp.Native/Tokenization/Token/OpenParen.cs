namespace plamp.Native.Tokenization.Token;

public class OpenParen : TokenBase
{
    public OpenParen(TokenPosition start, TokenPosition end) : base(start, end, ")")
    {
    }
}