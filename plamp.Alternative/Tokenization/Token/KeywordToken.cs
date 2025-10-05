using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;

namespace plamp.Alternative.Tokenization.Token;

public class KeywordToken(string stringValue, FilePosition position, Keywords keyword) : TokenBase(position, stringValue)
{
    public Keywords Keyword { get; } = keyword;
}