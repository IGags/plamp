using System;
using System.Reflection;

namespace plamp.Alternative;

internal class ParameterInfoImpl(Type type, string name) : ParameterInfo
{
    public override Type ParameterType { get; } = type;

    public override string Name { get; } = name;
}