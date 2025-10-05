using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class EndOfStatement(FilePosition position) : TokenBase(position, ";");