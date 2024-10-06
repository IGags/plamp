namespace plamp.Native.Tokenization.Token;

public class StringLiteral : TokenBase
{
    private readonly string _value;

    public StringLiteral(string value, int position, int end) : base(position, end)
    {
        _value = value;
    }

    public override string GetString() => _value;
}