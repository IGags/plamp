using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

/// <summary>
/// Двоеточие
/// </summary>
public class Colon(FilePosition position) : TokenBase(position, ":");