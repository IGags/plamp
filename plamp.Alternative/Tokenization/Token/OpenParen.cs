using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class OpenParen(FilePosition position) : TokenBase(position, "(");