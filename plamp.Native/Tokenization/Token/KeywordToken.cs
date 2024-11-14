using plamp.Native.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class KeywordToken : TokenBase
{
    public Keywords Keyword { get; }

    public KeywordToken(string stringValue, TokenPosition start, TokenPosition end, Keywords keyword) : base(start, end, stringValue)
    {
        Keyword = keyword;
    }
}