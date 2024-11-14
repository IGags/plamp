namespace plamp.Native.Tokenization.Token;

public class Operator : TokenBase
{
    public Operator(string op, TokenPosition start, TokenPosition end) : base(start, end, op) { }
}