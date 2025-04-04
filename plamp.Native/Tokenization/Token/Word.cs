using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class Word : TokenBase
{
    public Word(string value, FilePosition start, FilePosition end) : base(start, end, value)
    {
    }
}