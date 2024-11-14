namespace plamp.Native.Tokenization.Token;

public class EndOfLine : TokenBase
{
    public EndOfLine(string token, TokenPosition start, TokenPosition end) : base(start, end, token)
    {
    }
}