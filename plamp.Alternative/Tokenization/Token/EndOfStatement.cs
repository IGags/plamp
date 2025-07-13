using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class EndOfStatement : TokenBase
{
    public EndOfStatement(FilePosition start, FilePosition end) : base(start, end, ";")
    {
    }
}