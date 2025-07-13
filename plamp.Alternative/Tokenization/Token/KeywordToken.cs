using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;

namespace plamp.Alternative.Tokenization.Token;

public class KeywordToken : TokenBase
{
    public Keywords Keyword { get; }

    public KeywordToken(string stringValue, FilePosition start, FilePosition end, Keywords keyword) : base(start, end, stringValue)
    {
        Keyword = keyword;
    }
}