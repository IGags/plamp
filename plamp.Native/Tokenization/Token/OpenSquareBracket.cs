using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class OpenSquareBracket : TokenBase
{
    public OpenSquareBracket(FilePosition start, FilePosition end) : base(start, end, "[")
    {
    }
}