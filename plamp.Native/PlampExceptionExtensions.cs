using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Native.Tokenization.Token;

namespace plamp.Native;

public static class PlampExceptionExtensions
{
    public static PlampException GetPlampException(this PlampExceptionRecord record, TokenBase token, string fileName, AssemblyName assemblyName)
    {
        return new PlampException(record, token.Start, token.End, fileName, assemblyName);
    }
}