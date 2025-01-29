using plamp.Ast;
using plamp.Native.Tokenization.Token;

namespace plamp.Native;

public static class PlampExceptionExtensions
{
    public static PlampException GetPlampException(this PlampExceptionRecord record, TokenBase token)
    {
        return new PlampException(record, token.Start, token.End);
    }
}