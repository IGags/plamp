using System;
using System.Linq;
using plamp.Assembly.Building.Interfaces;
using plamp.Assembly.Models;

namespace plamp.Assembly.Building;

internal class TypeBuilderFluentSyntax<T>(TypeBuilderFluentSyntax inner)
    : IAfterTypeInfoNameBuilder<T>, IAliasBuilder<T>
{
    public IMemberBuilder<T> As(string alias)
    {
        var res = (MemberBuilderSyntax)inner.As(alias);
        return new MemberBuilderSyntax<T>(res);
    }

    public IMemberBuilder<T> WithMembers()
    {
        return new MemberBuilderSyntax<T>(new MemberBuilderSyntax(inner));
    }

    public IModuleBuilderSyntax CompleteType()
    {
        return inner.ModuleBuilder;
    }
}

internal class TypeBuilderFluentSyntax(DefaultTypeInfo typeInfo, ModuleBuilderSyntax builder) : IAfterTypeInfoNameBuilder, IAliasBuilder
{
    internal DefaultTypeInfo TypeInfo => typeInfo;

    internal ModuleBuilderSyntax ModuleBuilder => builder;

    public IMemberBuilder As(string alias)
    {
        builder.ThrowDuplicateModuleAlias(alias, typeInfo.Type);
        builder.ThrowMemberNameEquality(alias, typeInfo.Type);
        typeInfo.Alias = alias;
        return new MemberBuilderSyntax(this);
    }

    public IMemberBuilder WithMembers()
    {
        return new MemberBuilderSyntax(this);
    }

    public IModuleBuilderSyntax CompleteType()
    {
        return ModuleBuilder;
    }
}