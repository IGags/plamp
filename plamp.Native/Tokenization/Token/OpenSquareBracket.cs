namespace plamp.Native.Tokenization.Token;

public class OpenSquareBracket : TokenBase
{
    public OpenSquareBracket(int position) : base(position, position)
    {
    }
    
    public override string GetString() => "[";
}