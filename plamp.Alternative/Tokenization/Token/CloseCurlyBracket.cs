using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class CloseCurlyBracket(FilePosition position) : TokenBase(position, "}");