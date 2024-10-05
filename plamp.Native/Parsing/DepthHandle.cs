using System;

namespace plamp.Native.Parsing;

internal sealed class DepthHandle : IDisposable
{
    private readonly Action _disposeAction;
    private readonly int _currentDepth;

    internal DepthHandle(Action disposeAction, int currentDepth)
    {
        _disposeAction = disposeAction;
        _currentDepth = currentDepth;
    }

    public static implicit operator int(DepthHandle handle)
    {
        return handle._currentDepth;
    }
    
    public void Dispose()
    {
        _disposeAction();
    }
}