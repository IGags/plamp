namespace plamp.Native.Tokenization.Token;

public class OpenParen : TokenBase
{
    public OpenParen(int position) : base(position, position)
    {
    }
    
    public override string GetString() => "(";
}