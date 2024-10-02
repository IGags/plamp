namespace plamp.Native.Token;

public class EOF : TokenBase
{
    public EOF(int position) : base(position, position)
    {
    }
    public override string GetString() => "\n";
}