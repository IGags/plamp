using System;

namespace plamp.Assembly.Building.Interfaces;

public interface IModuleBuilderSyntax
{
    public IAfterTypeInfoNameBuilder<T> AddType<T>();

    public IAfterTypeInfoNameBuilder AddType(Type type);

    public IAfterTypeInfoNameBuilder<T> AddGenericTypeDefinition<T>();

    public IAfterTypeInfoNameBuilder AddGenericTypeDefinition(Type type);

    public IContainerBuilder CompleteModule();
}