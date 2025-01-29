using plamp.Ast.Node;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing.Symbols;

/// <summary>
/// Record that represent symbol own tokens(without children) and child nodes
/// </summary>
internal record struct PlampNativeSymbolRecord(NodeBase[] Children, TokenBase[] Tokens);