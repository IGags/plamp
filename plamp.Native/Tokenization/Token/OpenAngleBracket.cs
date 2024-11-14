using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class OpenAngleBracket : OperatorToken
{
    public OpenAngleBracket(string token, TokenPosition start, TokenPosition end) : base(token, start, end, OperatorEnum.Lesser)
    {
    }
}