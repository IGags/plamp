namespace plamp.Native.Token;

public class Scope : TokenBase
{
    public Scope(int position, int length) : base(position, position + length - 1)
    {
    }
    
    public override string GetString() => "\t";
}