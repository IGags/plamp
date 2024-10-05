using System;

namespace plamp.Native.Parsing;

internal sealed class DepthCounter
{
    private int _depth;
    
    internal DepthCounter(int depth)
    {
        if (depth < 0)
        {
            throw new ArgumentException();
        }
        
        _depth = depth;
    }

    public DepthHandle EnterNewScope()
    {
        _depth++;
        return new DepthHandle(() => _depth--, _depth);
    }

    public static implicit operator DepthHandle(DepthCounter counter)
    {
        return new DepthHandle(() => { }, counter._depth);
    }

    public static implicit operator DepthCounter(int depth)
    {
        return new DepthCounter(depth);
    }
    
    public static implicit operator int(DepthCounter depth)
    {
        return depth._depth;
    }
}