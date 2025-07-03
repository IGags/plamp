using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;

internal record TokenizationContext(
    string FileName,
    string[] Rows,
    List<TokenBase> Tokens,
    List<PlampException> Exceptions,
    AssemblyName AssemblyName);