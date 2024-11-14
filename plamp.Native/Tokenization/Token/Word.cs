namespace plamp.Native.Tokenization.Token;

public class Word : TokenBase
{
    public Word(string value, TokenPosition start, TokenPosition end) : base(start, end, value)
    {
    }
}