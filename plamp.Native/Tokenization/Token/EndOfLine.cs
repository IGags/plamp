using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class EndOfLine : TokenBase
{
    public EndOfLine(string token, FilePosition start, FilePosition end) : base(start, end, token)
    {
    }
}