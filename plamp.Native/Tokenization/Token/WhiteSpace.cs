namespace plamp.Native.Tokenization.Token;

public class WhiteSpace : TokenBase
{
    private readonly string _inner;

    public WhiteSpace(string inner, int position) : base(position, position)
    {
        _inner = inner;
    }
    
    public override string GetString() => _inner;
}