namespace plamp.Native.Tokenization.Token;

public class EndOfLine : TokenBase
{
    public EndOfLine(int position, int length) : base(position, position + length - 1)
    {
    }
    public override string GetString() => EndPosition - StartPosition == 1 
        ? PlampNativeTokenizer.EndOfLineCrlf : PlampNativeTokenizer.EndOfLine.ToString();
}