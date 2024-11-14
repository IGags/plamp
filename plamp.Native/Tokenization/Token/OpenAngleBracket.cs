namespace plamp.Native.Tokenization.Token;

public class OpenAngleBracket : Operator
{
    public OpenAngleBracket(string bracket, TokenPosition start, TokenPosition end) : base("<", start, end)
    {
    }
}