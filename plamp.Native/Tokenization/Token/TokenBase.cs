namespace plamp.Native.Tokenization.Token;

public abstract class TokenBase
{
    protected string StringValue { get; set; }
    public TokenPosition Start { get; }
    public TokenPosition End { get; }

    protected TokenBase(TokenPosition start, TokenPosition end, string stringValue)
    {
        Start = start;
        End = end;
        StringValue = stringValue;
    }
    
    public virtual string GetStringRepresentation() => StringValue;
}