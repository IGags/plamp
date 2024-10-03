namespace plamp.Native.Tokenization.Token;

public class Operator : TokenBase
{
    private readonly string _op;

    public Operator(string op, int position) : base(position, op.Length - 1 + position)
    {
        _op = op;
    }

    public override string GetString() => _op;
}