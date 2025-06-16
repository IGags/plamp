using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Compiler.Util;

public class AssemblyNameEqualityComparer : IEqualityComparer<AssemblyName>
{
    public bool Equals(AssemblyName x, AssemblyName y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return
            x.ContentType == y.ContentType
            && Equals(x.CultureInfo, y.CultureInfo)
            && x.CultureName == y.CultureName
            && x.Flags == y.Flags
            && x.FullName == y.FullName
            && x.Name == y.Name
            && Equals(x.Version, y.Version);
    }

    public int GetHashCode(AssemblyName obj)
    {
        var hashCode = new HashCode();
        hashCode.Add((int)obj.ContentType);
        hashCode.Add(obj.CultureInfo);
        hashCode.Add(obj.CultureName);
        hashCode.Add((int)obj.Flags);
        hashCode.Add(obj.FullName);
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Version);
        return hashCode.ToHashCode();
    }
}