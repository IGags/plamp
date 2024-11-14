namespace plamp.Native.Tokenization.Token;

public class CloseSquareBracket : TokenBase
{
    public CloseSquareBracket(TokenPosition start, TokenPosition end) : base(start, end, "]")
    {
    }

    public override string GetStringRepresentation() => "]";
}