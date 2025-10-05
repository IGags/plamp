using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class Word(string value, FilePosition position) : TokenBase(position, value);