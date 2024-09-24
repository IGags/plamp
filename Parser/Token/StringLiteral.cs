namespace Parser.Token;

public class StringLiteral : TokenBase
{
    private readonly string _value;

    public StringLiteral(string value)
    {
        _value = value;
    }

    public override string GetString() => _value;
}