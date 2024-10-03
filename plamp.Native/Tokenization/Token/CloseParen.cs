namespace plamp.Native.Tokenization.Token;

public class CloseParen : TokenBase
{
    public CloseParen(int position) : base(position, position) {}
    
    public override string GetString() => ")";
}