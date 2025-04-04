using plamp.Abstractions.Ast;
using plamp.Native.Tokenization.Enumerations;

namespace plamp.Native.Tokenization.Token;

public class CloseAngleBracket : OperatorToken
{
    public CloseAngleBracket(string token, FilePosition start, FilePosition end) : base(token, start, end, OperatorEnum.Greater)
    {
    }
}