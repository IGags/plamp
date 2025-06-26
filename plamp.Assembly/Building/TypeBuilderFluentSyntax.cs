using System;
using System.Linq;
using plamp.Assembly.Building.Interfaces;
using plamp.Assembly.Models;

namespace plamp.Assembly.Building;

internal class TypeBuilderFluentSyntax<T>(DefaultTypeInfo typeInfo, ModuleBuilderSyntax builder)
    : IAfterTypeInfoNameBuilder<T>, IAliasBuilder<T>
{
    internal DefaultTypeInfo TypeInfo => typeInfo;

    internal ModuleBuilderSyntax ModuleBuilder => builder;

    public IMemberBuilder<T> As(string alias)
    {
        builder.ThrowDuplicateModuleAlias(alias, typeInfo.Type);
        builder.ThrowMemberNameEquality(alias, typeInfo.Type);
        typeInfo.Alias = alias;
        return new MemberBuilderSyntax<T>(this);
    }

    public IMemberBuilder<T> WithMembers()
    {
        return new MemberBuilderSyntax<T>(this);
    }

    public IModuleBuilderSyntax CompleteType()
    {
        return ModuleBuilder;
    }
}