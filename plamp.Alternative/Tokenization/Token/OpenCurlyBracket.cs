using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class OpenCurlyBracket(FilePosition position) : TokenBase(position, "{");