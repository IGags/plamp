using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class Comma(FilePosition position) : TokenBase(position, ",");