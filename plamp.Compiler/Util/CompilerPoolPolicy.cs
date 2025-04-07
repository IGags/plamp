using System;
using Microsoft.Extensions.ObjectPool;

namespace plamp.Compiler;

public class CompilerPoolPolicy<TResource> : IPooledObjectPolicy<TResource> where TResource : class
{
    private readonly Func<TResource> _factory;

    public CompilerPoolPolicy(Func<TResource> factory)
    {
        _factory = factory;
    }
    
    public TResource Create() => _factory();

    public bool Return(TResource obj) => true;
}