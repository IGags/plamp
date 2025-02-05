using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node.Body;

public record DefNode(NodeBase ReturnType, MemberNode Name, List<ParameterNode> ParameterList, BodyNode Body) 
    : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ReturnType;
        yield return Name;
        if (ParameterList != null)
        {
            foreach (var parameter in ParameterList)
            {
                yield return parameter;
            }
        }

        yield return Body;
    }

    //Not scary, much better
    public virtual bool Equals(DefNode other)
    {
        if (other == null) return false;
        return ReturnType == other.ReturnType && Name == other.Name
            && ParameterList.SequenceEqual(other.ParameterList)
            && Body.Equals(other.Body);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(ReturnType);
        hashCode.Add(Name);
        foreach (var parameter in ParameterList)
        {
            hashCode.Add(parameter);
        }
        hashCode.Add(Body);
        return hashCode.ToHashCode();
    }
}