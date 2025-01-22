using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record DefNode(NodeBase ReturnType, MemberNode Name, List<ParameterNode> ParameterList, BodyNode Body) : NodeBase
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

    //Sacry
    public virtual bool Equals(DefNode other)
    {
        if (other == null) return false;
        if (ReturnType == null && other.ReturnType != null) return false;
        if (ReturnType != null && other.ReturnType == null) return false;
        if (Name == null && other.Name != null) return false;
        if (Name != null && other.Name == null) return false;
        if (ParameterList == null && other.ParameterList != null) return false;
        if (ParameterList != null && other.ParameterList == null) return false;
        if (Body == null && other.Body != null) return false;
        if (Body != null && other.Body == null) return false;
        if (ReturnType != null && !ReturnType.Equals(other.ReturnType)) return false;
        if (Name != null && !Name.Equals(other.Name)) return false;
        if (Body != null && !Body.Equals(other.Body)) return false;
        if (ParameterList != null && ParameterList.Count != other.ParameterList!.Count) return false;
        if (ParameterList != null)
        {
            for (var i = 0; i < ParameterList.Count; i++)
            {
                if (ParameterList[i] == null && other.ParameterList![i] == null)
                {
                    continue;
                }
                if (!ParameterList[i].Equals(other.ParameterList![i])) return false;
            }
        }
        return true;
    }
}