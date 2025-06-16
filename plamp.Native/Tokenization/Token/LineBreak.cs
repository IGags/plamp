using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class LineBreak : TokenBase
{
    public LineBreak(string stringValue, FilePosition start, FilePosition end) : base(start, end, stringValue)
    {
    }
}