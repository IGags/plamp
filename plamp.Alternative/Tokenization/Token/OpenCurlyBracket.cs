using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class OpenCurlyBracket : TokenBase
{
    public OpenCurlyBracket(FilePosition start, FilePosition end) : base(start, end, "{")
    {
    }
}