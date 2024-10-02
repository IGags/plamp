namespace plamp.Native.Token;

public abstract class TokenBase
{
    public int StartPosition { get; }
    public int EndPosition { get; }

    protected TokenBase(int startPosition, int endPosition)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
    }
    
    public abstract string GetString();
}