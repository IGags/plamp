namespace plamp.Native.Tokenization.Token;

public class StringLiteral : TokenBase
{
    private readonly string _value;

    public StringLiteral(string value, int position) : base(position, position + value.Length + 1)
    {
        _value = value;
    }

    public override string GetString() => _value;
}