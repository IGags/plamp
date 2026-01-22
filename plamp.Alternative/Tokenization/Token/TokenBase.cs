using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public abstract class TokenBase(FilePosition position, string stringValue)
{
    protected string StringValue { get; set; } = stringValue;
    public FilePosition Position { get; } = position;

    public virtual string GetStringRepresentation() => StringValue;
}