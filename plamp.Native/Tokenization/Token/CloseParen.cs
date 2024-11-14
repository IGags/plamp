namespace plamp.Native.Tokenization.Token;

public class CloseParen : TokenBase
{
    public CloseParen(TokenPosition start, TokenPosition end) : base(start, end, ")") {}
}