namespace plamp.Native.Tokenization.Token;

public class OpenBracket : TokenBase
{
    public OpenBracket(int position) : base(position, position)
    {
    }
    
    public override string GetString() => "(";
}