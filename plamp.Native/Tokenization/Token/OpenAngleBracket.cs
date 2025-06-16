using plamp.Abstractions.Ast;
using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class OpenAngleBracket : OperatorToken
{
    public OpenAngleBracket(string token, FilePosition start, FilePosition end) : base(token, start, end, OperatorEnum.Lesser)
    {
    }
}