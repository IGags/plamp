using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class EndOfFile : TokenBase
{
    public EndOfFile(FilePosition start, FilePosition end) : base(start, end, "EOF")
    {
    }
}