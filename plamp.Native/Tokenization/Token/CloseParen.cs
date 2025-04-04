using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class CloseParen : TokenBase
{
    public CloseParen(FilePosition start, FilePosition end) : base(start, end, ")") {}
}