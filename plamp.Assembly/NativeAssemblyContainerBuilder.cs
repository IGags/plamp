using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Assembly.Models;

namespace plamp.Assembly;

internal class NativeAssemblyContainerBuilder : IContainerBuilder
{
    internal Dictionary<Type, DefaultTypeInfo> TypeInfoDict { get; } = [];

    internal Dictionary<DefaultTypeInfo, List<MethodInfo>> MethodInfoDict { get; } = [];
    
    public IAfterTypeInfoNameBuilder<T> AddType<T>()
    {
        if (TypeInfoDict.ContainsKey(typeof(T))) throw new ArgumentException("Type already exists in container");
        var typeInfo = new DefaultTypeInfo{ Type = typeof(T) };
        TypeInfoDict.Add(typeof(T), typeInfo);
        return new TypeBuilderFluentSyntax<T>(typeInfo, this);
    }

    public static IContainerBuilder CreateContainerBuilder()
    {
        return new NativeAssemblyContainerBuilder();
    }
}

internal class TypeBuilderFluentSyntax<T> : IAfterTypeInfoNameBuilder<T>, IAliasBuilder<T>, IModuleNameBuilder<T>
{
    private readonly DefaultTypeInfo _typeInfo;
    private readonly NativeAssemblyContainerBuilder _builder;

    public TypeBuilderFluentSyntax(DefaultTypeInfo typeInfo, NativeAssemblyContainerBuilder builder)
    {
        _typeInfo = typeInfo;
        _builder = builder;
    }
    
    IModuleNameBuilder<T> IAfterTypeInfoNameBuilder<T>.As(string alias)
    {
        _typeInfo.Alias = alias;
        return this;
    }

    IAliasBuilder<T> IAfterTypeInfoNameBuilder<T>.FromModule(string moduleName)
    {
        _typeInfo.Module = moduleName;
        return this;
    }

    public IMemberBuilder<T> WithMembers()
    {
        return new MemberBuilderSyntax<T>(_builder, _typeInfo);
    }

    IMemberBuilder<T> IAliasBuilder<T>.As(string alias)
    {
        _typeInfo.Alias = alias;
        return new MemberBuilderSyntax<T>(_builder, _typeInfo);
    }

    IMemberBuilder<T> IModuleNameBuilder<T>.FromModule(string moduleName)
    {
        _typeInfo.Module = moduleName;
        return new MemberBuilderSyntax<T>(_builder, _typeInfo);
    }
}

internal class MemberBuilderSyntax<T> : IOptionalAliasBuilder<T>, IMemberBuilder<T>
{
    private readonly DefaultTypeInfo _typeInfo;
    private readonly NativeAssemblyContainerBuilder _builder;
    private Action<string> _aliasAssignmentFn;
    
    public MemberBuilderSyntax(NativeAssemblyContainerBuilder builder, DefaultTypeInfo typeInfo)
    {
        _builder = builder;
        _typeInfo = typeInfo;
    }


    public IOptionalAliasBuilder<T> AddMethod(Expression<Action<T>> methodExpression)
    {
        throw new NotImplementedException();
    }

    public IOptionalAliasBuilder<T> AddMethod(Expression<Func<T, object>> methodExpression)
    {
        throw new NotImplementedException();
    }

    public IOptionalAliasBuilder<T> AddMethod(MethodInfo methodInfo)
    {
        throw new NotImplementedException();
    }

    public IMemberBuilder<T> AddCtor(Expression<Func<T>> ctorExpression)
    {
        throw new NotImplementedException();
    }

    public IMemberBuilder<T> AddCtor(ConstructorInfo constructorInfo)
    {
        throw new NotImplementedException();
    }

    public IOptionalAliasBuilder<T> AddField(Expression<Func<T, object>> fieldExpression)
    {
        throw new NotImplementedException();
    }

    public IOptionalAliasBuilder<T> AddField(FieldInfo fieldInfo)
    {
        throw new NotImplementedException();
    }

    public IOptionalAliasBuilder<T> AddProperty(Expression<Func<T, object>> propertyExpression)
    {
        throw new NotImplementedException();
    }

    public IOptionalAliasBuilder<T> AddProperty(PropertyInfo propertyInfo)
    {
        throw new NotImplementedException();
    }

    public IMemberBuilder<T> As(string alias)
    {
        _aliasAssignmentFn(alias);
        return this;
    }
}

public interface IContainerBuilder
{
    public IAfterTypeInfoNameBuilder<T> AddType<T>();
}

public interface IAfterTypeInfoNameBuilder<T>
{
    public IModuleNameBuilder<T> As(string alias);
    
    public IAliasBuilder<T> FromModule(string moduleName);

    public IMemberBuilder<T> WithMembers();
}

public interface IAliasBuilder<T>
{
    public IMemberBuilder<T> As(string alias);
}

public interface IModuleNameBuilder<T>
{
    public IMemberBuilder<T> FromModule(string moduleName);
}

public interface IOptionalAliasBuilder<T> : IMemberBuilder<T>
{
    public IMemberBuilder<T> As(string alias);
}

public interface IMemberBuilder<T>
{
    public IOptionalAliasBuilder<T> AddMethod(Expression<Action<T>> methodExpression);

    public IOptionalAliasBuilder<T> AddMethod(Expression<Func<T, object>> methodExpression);

    public IOptionalAliasBuilder<T> AddMethod(MethodInfo methodInfo);

    public IMemberBuilder<T> AddCtor(Expression<Func<T>> ctorExpression);

    public IMemberBuilder<T> AddCtor(ConstructorInfo constructorInfo);

    public IOptionalAliasBuilder<T> AddField(Expression<Func<T, object>> fieldExpression);

    public IOptionalAliasBuilder<T> AddField(FieldInfo fieldInfo);

    public IOptionalAliasBuilder<T> AddProperty(Expression<Func<T, object>> propertyExpression);

    public IOptionalAliasBuilder<T> AddProperty(PropertyInfo propertyInfo);
}