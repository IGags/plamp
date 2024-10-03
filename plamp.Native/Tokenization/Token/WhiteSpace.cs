namespace plamp.Native.Tokenization.Token;

public class WhiteSpace : TokenBase
{
    public WhiteSpace(int position) : base(position, position)
    {
    }
    
    public override string GetString() => " ";
}