namespace plamp.Native.Tokenization.Token;

public class CloseBracket : TokenBase
{
    public CloseBracket(int position) : base(position, position) {}
    
    public override string GetString() => ")";
}