using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class ImplicitEndOfStatement(FilePosition position, string value) : TokenBase(position, value);