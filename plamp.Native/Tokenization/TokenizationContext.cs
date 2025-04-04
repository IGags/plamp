using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization;

internal record TokenizationContext(
    string FileName,
    string[] Rows,
    List<TokenBase> Tokens,
    List<PlampException> Exceptions,
    AssemblyName AssemblyName);