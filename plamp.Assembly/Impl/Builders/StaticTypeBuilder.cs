using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Assembly.Impl.Models;

namespace plamp.Assembly.Impl.Builders;

public class StaticTypeBuilder
{
    public Type Type { get; }

    private readonly List<ExternalMemberInfo> _members = [];
    
    internal IReadOnlyList<ExternalMemberInfo> Members => _members;

    internal StaticTypeBuilder(Type type)
    {
        Type = type;
    }
    
    public void DefineMethod(MethodInfo method, string alias = null) => DefineInternal(method, alias);

    public void DefineProperty(PropertyInfo property, string alias = null) => DefineInternal(property, alias);

    public void DefineFiled(FieldInfo field, string alias = null) => DefineInternal(field, alias);

    private void DefineInternal(MemberInfo member, string alias = null)
    {
        if (member.DeclaringType == null || member.DeclaringType.IsAssignableTo(Type))
        {
            throw new InvalidOperationException($"Member declaring type must be inherited from the {Type.Name}");
        }
        
        alias ??= member.Name;
        var existingMember = _members.FirstOrDefault(x => x.Member.Equals(member));
        
        if (existingMember != default)
        {
            if(alias.Equals(existingMember.Alias)) return;
            throw new InvalidOperationException(
                $"Cannot declare member with this signature. Member with alias '{alias}' already declared.");
        }
        
        var existMember = _members.FirstOrDefault(x => x.Alias.Equals(alias));
        if (existMember != default)
        {
            if (existMember.Member is not MethodInfo)
            {
                throw new InvalidOperationException($"Member with alias '{alias}' already declared.");
            }
            
            var info = new ExternalMemberInfo(member, alias);
            _members.Add(info);
            return;
        }
        
        var memberInfo = new ExternalMemberInfo(member, alias);
        _members.Add(memberInfo);
    }
}