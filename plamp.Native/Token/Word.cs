namespace Parser.Token;

public class Word : TokenBase
{
    private readonly string _value;

    public Word(string value)
    {
        _value = value;
    }

    public override string GetString() => _value;
}