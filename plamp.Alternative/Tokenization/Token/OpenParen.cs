using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class OpenParen : TokenBase
{
    public OpenParen(FilePosition start, FilePosition end) : base(start, end, "(")
    {
    }
}