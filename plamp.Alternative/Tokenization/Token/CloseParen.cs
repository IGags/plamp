using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class CloseParen : TokenBase
{
    public CloseParen(FilePosition start, FilePosition end) : base(start, end, ")") {}
}