namespace plamp.Native.Tokenization.Token;

public class CloseSquareBracket : TokenBase
{
    public CloseSquareBracket(int position) : base(position, position)
    {
    }

    public override string GetString() => "]";
}