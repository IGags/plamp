using plamp.Ast;

namespace plamp.Native.Tokenization.Token;

public class CloseSquareBracket : TokenBase
{
    public CloseSquareBracket(FilePosition start, FilePosition end) : base(start, end, "]")
    {
    }

    public override string GetStringRepresentation() => "]";
}