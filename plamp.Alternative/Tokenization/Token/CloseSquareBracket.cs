using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class CloseSquareBracket(FilePosition position) : TokenBase(position, "]")
{
    public override string GetStringRepresentation() => "]";
}