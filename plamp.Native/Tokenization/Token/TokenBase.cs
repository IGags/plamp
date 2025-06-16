using plamp.Abstractions.Ast;

namespace plamp.Native.Tokenization.Token;

public abstract class TokenBase
{
    protected string StringValue { get; set; }
    public FilePosition Start { get; }
    public FilePosition End { get; }

    protected TokenBase(FilePosition start, FilePosition end, string stringValue)
    {
        Start = start;
        End = end;
        StringValue = stringValue;
    }
    
    public virtual string GetStringRepresentation() => StringValue;
}