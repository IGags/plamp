using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class CloseSquareBracket : TokenBase
{
    public CloseSquareBracket(FilePosition start, FilePosition end) : base(start, end, "]")
    {
    }

    public override string GetStringRepresentation() => "]";
}