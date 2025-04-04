using System.Reflection;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;

namespace plamp.Native.Parsing;

internal record ParsingContext(
    TokenSequence TokenSequence,
    DepthCounter Depth,
    ParsingTransactionSource TransactionSource,
    AssemblyName AssemblyName,
    string FileName);