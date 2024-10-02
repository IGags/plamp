namespace plamp.Native.Token;

public class CloseSquareBracket : TokenBase
{
    public CloseSquareBracket(int position) : base(position, position)
    {
    }

    public override string GetString() => "]";
}