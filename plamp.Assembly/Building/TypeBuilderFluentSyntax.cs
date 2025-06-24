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
        if (builder.DefinedTypes.Any(x => alias.Equals(x.Alias))
            || builder.DefinedTypes.Any(x => x.Type.Name.Equals(alias)))
        {
            throw new ArgumentException($"Type with the name {alias} already declared in this module");
        }
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