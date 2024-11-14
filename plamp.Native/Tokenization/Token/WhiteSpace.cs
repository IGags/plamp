namespace plamp.Native.Tokenization.Token;

public class WhiteSpace : TokenBase
{
    public WhiteSpaceKind Kind { get; }

    public WhiteSpace(string inner, TokenPosition start, TokenPosition end, WhiteSpaceKind kind) : base(start, end, inner)
    {
        Kind = kind;
    }
}