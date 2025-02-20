using plamp.Ast;
using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class KeywordToken : TokenBase
{
    public Keywords Keyword { get; }

    public KeywordToken(string stringValue, FilePosition start, FilePosition end, Keywords keyword) : base(start, end, stringValue)
    {
        Keyword = keyword;
    }
}