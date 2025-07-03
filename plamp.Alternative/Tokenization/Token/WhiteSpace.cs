using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;

namespace plamp.Alternative.Tokenization.Token;

public class WhiteSpace : TokenBase
{
    public WhiteSpaceKind Kind { get; }

    public WhiteSpace(string inner, FilePosition start, FilePosition end, WhiteSpaceKind kind) : base(start, end, inner)
    {
        Kind = kind;
    }
}