using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class EndOfFile(FilePosition position) : TokenBase(position, "EOF");