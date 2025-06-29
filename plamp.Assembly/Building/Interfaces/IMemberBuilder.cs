using System;
using System.Linq.Expressions;
using System.Reflection;

namespace plamp.Assembly.Building.Interfaces;

public interface IMemberBuilder<T>
{
    public IOptionalAliasBuilder<T> AddMethod(Expression<Action<T>> methodExpression);

    public IOptionalAliasBuilder<T> AddMethod(Expression<Func<T, object>> methodExpression);

    public IOptionalAliasBuilder<T> AddMethod(MethodInfo methodInfo);

    public IMemberBuilder<T> AddCtor(Expression<Func<T>> ctorExpression);

    public IMemberBuilder<T> AddCtor(ConstructorInfo constructorInfo);

    public IOptionalAliasBuilder<T> AddPropertyOrField(Expression<Func<T, object>> memberExpression);

    public IOptionalAliasBuilder<T> AddField(FieldInfo fieldInfo);

    public IOptionalAliasBuilder<T> AddProperty(PropertyInfo propertyInfo);

    public IMemberBuilder<T> AddIndexer(Expression<Func<T, object>> indexerExpression);

    public IMemberBuilder<T> AddIndexer(PropertyInfo propertyInfo);

    public IModuleBuilderSyntax CompleteType();
}

public interface IMemberBuilder
{
    public IOptionalAliasBuilder AddMethod(MethodInfo methodInfo);
    
    public IMemberBuilder AddCtor(ConstructorInfo constructorInfo);
    
    public IOptionalAliasBuilder AddField(FieldInfo fieldInfo);

    public IOptionalAliasBuilder AddProperty(PropertyInfo propertyInfo);
    
    public IMemberBuilder AddIndexer(PropertyInfo propertyInfo);

    public IModuleBuilderSyntax CompleteType();
}