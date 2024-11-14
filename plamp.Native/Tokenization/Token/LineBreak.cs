namespace plamp.Native.Tokenization.Token;

public class LineBreak : TokenBase
{
    public LineBreak(string stringValue, TokenPosition start, TokenPosition end) : base(start, end, stringValue)
    {
    }
}