namespace plamp.Native.Tokenization.Token;

public class EndOfLine : TokenBase
{
    public EndOfLine(int position) : base(position, position)
    {
    }
    public override string GetString() => "\n";
}