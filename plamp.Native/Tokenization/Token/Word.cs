namespace plamp.Native.Token;

public class Word : TokenBase
{
    private readonly string _value;

    public Word(string value, int position) : base(position, position + value.Length - 1)
    {
        _value = value;
    }

    public override string GetString() => _value;
}