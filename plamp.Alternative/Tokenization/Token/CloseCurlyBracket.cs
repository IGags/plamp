using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class CloseCurlyBracket : TokenBase
{
    public CloseCurlyBracket(FilePosition start, FilePosition end) : base(start, end, "}")
    {
    }
}