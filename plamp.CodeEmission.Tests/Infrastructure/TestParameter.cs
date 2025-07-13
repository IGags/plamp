using System.Reflection;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class TestParameter : ParameterInfo
{
    public override Type ParameterType { get; }

    public override string Name { get; }

    public TestParameter(Type type, string name)
    {
        ParameterType = type;
        Name = name;
    }
}