using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;

namespace plamp.Alternative.Tokenization.Token;

public class WhiteSpace(string inner, FilePosition position, WhiteSpaceKind kind) : TokenBase(position, inner)
{
    public WhiteSpaceKind Kind { get; } = kind;
}