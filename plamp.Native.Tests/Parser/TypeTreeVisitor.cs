using System;
using System.Collections.Generic;
using plamp.Ast;
using plamp.Ast.Node;
using Xunit;

namespace plamp.Native.Tests.Parser;

public class TypeTreeVisitor : BaseVisitor
{
    private readonly Type[] _expectedTypeSequence;
    private int _pos;
    private readonly IEnumerator<string> _memberNameEnumerator;

    public TypeTreeVisitor(Type[] expectedTypeSequence, List<string> memberNameList)
    {
        _expectedTypeSequence = expectedTypeSequence;
        _memberNameEnumerator = memberNameList.GetEnumerator();
    }
    
    protected override void VisitNodeBase(NodeBase node)
    {
        Assert.True(_expectedTypeSequence.Length > _pos);
        Assert.Equal(_expectedTypeSequence[_pos], node.GetType());
        _pos++;
    }

    protected override void VisitMember(MemberNode member)
    {
        var isMove = _memberNameEnumerator.MoveNext();
        Assert.True(isMove);
        Assert.Equal(_memberNameEnumerator.Current, member.MemberName);
    }
    
    public void Validate()
    {
        Assert.Equal(_expectedTypeSequence.Length, _pos);
        Assert.False(_memberNameEnumerator.MoveNext());
    }
}