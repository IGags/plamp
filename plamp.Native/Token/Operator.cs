namespace Parser.Token;

public class Operator : TokenBase
{
    private readonly string _op;

    public Operator(string op)
    {
        _op = op;
    }

    public override string GetString() => _op;
}