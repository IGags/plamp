using System;

namespace plamp.Assembly.Building.Interfaces;

public interface IModuleBuilderSyntax
{
    public IAfterTypeInfoNameBuilder<T> AddType<T>();

    public IAfterTypeInfoNameBuilder<T> AddGenericTypeDefinition<T>();

    public IContainerBuilder CompleteModule();
}