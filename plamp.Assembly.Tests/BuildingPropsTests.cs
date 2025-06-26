using System;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingPropsTests
{
    // public void AddProperyInfo
}

public class ExamplePropClass<T>
{
    public int SimpleProperty { get; set; }
    
    public T GenericProperty { get; set; } = default!;
    
    private int _initProperty;
    
    //Set-only property isn't supported yet
    public int InitProperty
    {
        set => _initProperty = value;
    }
}