namespace plamp.Native.Token;

public class Comma : TokenBase
{
    public Comma(int position) : base(position, position)
    {
    }
    public override string GetString() => ",";
}