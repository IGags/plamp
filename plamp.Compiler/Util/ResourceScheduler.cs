using System;
using Microsoft.Extensions.ObjectPool;
using plamp.Abstractions;
using plamp.Abstractions.Compilation;

namespace plamp.Compiler.Util;

public class ResourceScheduler<TResource> where TResource : class
{
    private readonly ResourceType _schedulerType;
    
    private readonly TResource _parallelResource;
    
    private readonly ObjectPool<TResource> _resourcePool;
    
    private readonly Func<TResource> _resourceFactory;
    
    private ResourceScheduler(TResource parallelResource, ResourceType type)
    {
        _parallelResource = parallelResource;
        _schedulerType = type;
    }

    private ResourceScheduler(ObjectPool<TResource> resourcePool, ResourceType type)
    {
        _resourcePool = resourcePool;
        _schedulerType = type;
    }

    private ResourceScheduler(Func<TResource> resourceFactory, ResourceType type)
    {
        _resourceFactory = resourceFactory;
        _schedulerType = type;
    }

    public static ResourceScheduler<TResource> CreateScheduler(
        Func<TResource> resourceCreator, 
        ICompilerEntity factoryEntity)
    {
        switch (factoryEntity.Type)
        {
            case ResourceType.Parallel:
                return new ResourceScheduler<TResource>(resourceCreator(), ResourceType.Parallel);
            case ResourceType.SingleThreaded:
                return new ResourceScheduler<TResource>(
                    new DefaultObjectPool<TResource>(new CompilerPoolPolicy<TResource>(resourceCreator)),
                    ResourceType.SingleThreaded);
            case ResourceType.Disposable:
                return new ResourceScheduler<TResource>(resourceCreator, ResourceType.Disposable);
        }
        
        throw new NotSupportedException($"Resource type {factoryEntity.Type} is not supported");
    }

    public TResource GetResource()
    {
        switch (_schedulerType)
        {
            case ResourceType.Parallel:
                return _parallelResource;
            case ResourceType.SingleThreaded:
                return _resourcePool.Get();
            case ResourceType.Disposable:
                return _resourceFactory();
        }
        
        throw new Exception("Unknown scheduler type");
    }

    public void Return(TResource resource)
    {
        if (_schedulerType == ResourceType.SingleThreaded)
        {
            _resourcePool.Return(resource);
        }
    }
}