using System;

namespace plamp.Abstractions.Symbols;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Struct)]
public class PlampVisibleAttribute : Attribute;