namespace Parser.Token;

public class EOF : TokenBase
{
    public override string GetString() => "\n";
}