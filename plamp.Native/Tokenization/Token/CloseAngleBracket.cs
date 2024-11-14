using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class CloseAngleBracket : OperatorToken
{
    public CloseAngleBracket(string token, TokenPosition start, TokenPosition end) : base(token, start, end, OperatorEnum.Greater)
    {
    }
}