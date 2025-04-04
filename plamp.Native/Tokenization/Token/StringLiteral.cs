using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public class StringLiteral : TokenBase
{
    public StringLiteral(string value, FilePosition start, FilePosition end) : base(start, end, value) { }
}