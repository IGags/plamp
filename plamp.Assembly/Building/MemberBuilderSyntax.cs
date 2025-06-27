using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Assembly.Building.Interfaces;
using plamp.Assembly.Models;

namespace plamp.Assembly.Building;

internal class MemberBuilderSyntax<T>(MemberBuilderSyntax inner) : IOptionalAliasBuilder<T>
{
    public IOptionalAliasBuilder<T> AddMethod(Expression<Action<T>> methodExpression)
    {
        if (methodExpression.Body is not MethodCallExpression call)
            throw ThrowInvalidMethodExpression(nameof(methodExpression));
        var info = GetMethodOrGenericDefinition(call.Method);
        return AddMethod(info);
    }

    public IOptionalAliasBuilder<T> AddMethod(Expression<Func<T, object>> methodExpression)
    {
        MethodInfo methodInfo;
        if (methodExpression.Body is MethodCallExpression simpleCall)
        {
            methodInfo = simpleCall.Method;
        }
        else if (methodExpression.Body is UnaryExpression 
                { NodeType: ExpressionType.Convert, Operand: MethodCallExpression call })
        {
            methodInfo = call.Method;
        }
        else
        {
            throw ThrowInvalidMethodExpression(nameof(methodExpression));
        }
        
        methodInfo = GetMethodOrGenericDefinition(methodInfo);
        return AddMethod(methodInfo);
    }

    public IOptionalAliasBuilder<T> AddMethod(MethodInfo methodInfo)
    {
        var syntax = (MemberBuilderSyntax)inner.AddMethod(methodInfo);
        return new MemberBuilderSyntax<T>(syntax);
    }

    public IMemberBuilder<T> AddCtor(Expression<Func<T>> ctorExpression)
    {
        const string msg = $"Argument {nameof(ctorExpression)} is not constructor call expression";
        if (ctorExpression.Body is not NewExpression newExpression)
        {
            throw new ArgumentException(msg);
        }

        var ctorInfo = newExpression.Constructor != null ? newExpression.Constructor : newExpression.Type.GetConstructors().FirstOrDefault();
        if (ctorInfo == null)
        {
            throw new ArgumentException(msg);
        }
        
        var type = inner.TypeBuilder.TypeInfo.Type;
        if (type.IsGenericTypeDefinition)
        {
            ctorInfo = (ConstructorInfo)MethodBase.GetMethodFromHandle(ctorInfo.MethodHandle, type.TypeHandle)!;
        }

        return AddCtor(ctorInfo);
    }

    public IMemberBuilder<T> AddCtor(ConstructorInfo constructorInfo)
    {
        var syntax = (MemberBuilderSyntax)inner.AddCtor(constructorInfo);
        return new MemberBuilderSyntax<T>(syntax);
    }

    public IOptionalAliasBuilder<T> AddPropertyOrField(Expression<Func<T, object>> memberExpression)
    {
        MemberInfo memberInfo;
        if (memberExpression.Body is MemberExpression member)
        {
            memberInfo = member.Member;
        }
        else if (memberExpression.Body is UnaryExpression
            {
                NodeType: ExpressionType.Convert, Operand: MemberExpression innerMember
            })
        {
            memberInfo = innerMember.Member;
        }
        else
        {
            throw new ArgumentException($"Argument {nameof(memberExpression)} is not member access expression");
        }

        var type = inner.TypeBuilder.TypeInfo.Type;
        if (type.IsGenericTypeDefinition)
        {
            //Fld or prop
            memberInfo = type.GetMember(memberInfo.Name).First();
        }
        
        return AddMemberInner(memberInfo);
    }

    public IOptionalAliasBuilder<T> AddField(FieldInfo fieldInfo)
    {
        var syntax = (MemberBuilderSyntax)inner.AddField(fieldInfo);
        return new MemberBuilderSyntax<T>(syntax);
    }

    public IOptionalAliasBuilder<T> AddProperty(PropertyInfo propertyInfo)
    {
        var syntax = (MemberBuilderSyntax)inner.AddProperty(propertyInfo);
        return new MemberBuilderSyntax<T>(syntax);
    }

    public IMemberBuilder<T> AddIndexer(Expression<Func<T, object>> indexerExpression)
    {
        MethodInfo indexerMethod;
        if (indexerExpression.Body is MethodCallExpression simpleCall)
        {
            indexerMethod = simpleCall.Method;
        }
        else if (indexerExpression.Body is UnaryExpression 
                     { NodeType: ExpressionType.Convert, Operand: MethodCallExpression call })
        {
            indexerMethod = call.Method;
        }
        else
        {
            throw ThrowInvalidIndexer(nameof(indexerExpression));
        }

        if (indexerMethod.IsGenericMethod)
        {
            throw ThrowInvalidIndexer(nameof(indexerExpression));
        }
        
        var type = inner.TypeBuilder.TypeInfo.Type;
        indexerMethod = GetMethodOrGenericDefinition(indexerMethod);
        
        var existingIndexers = type
            .GetProperties()
            .Where(x => x.GetIndexParameters().Length > 0)
            .Where(x => x.CanWrite);
        
        //Need to throw
        var indexerProperty = existingIndexers.SingleOrDefault(x => x.GetGetMethod() == indexerMethod);
        if (indexerProperty == null)
        {
            throw ThrowInvalidIndexer(nameof(indexerExpression));
        }
        
        return AddIndexer(indexerProperty);
    }

    public IMemberBuilder<T> AddIndexer(PropertyInfo propertyInfo)
    {
        var syntax = (MemberBuilderSyntax)inner.AddIndexer(propertyInfo);
        return new MemberBuilderSyntax<T>(syntax);
    }

    public IModuleBuilderSyntax CompleteType() => inner.CompleteType();

    public IMemberBuilder<T> As(string alias)
    {
        var syntax = (MemberBuilderSyntax)inner.As(alias);
        return new MemberBuilderSyntax<T>(syntax);
    }
    
    private MethodInfo GetMethodOrGenericDefinition(MethodInfo methodInfo)
    {
        if (methodInfo.IsConstructedGenericMethod)
        {
            methodInfo = methodInfo.GetGenericMethodDefinition();
        }

        if (inner.TypeBuilder.TypeInfo.Type.IsGenericTypeDefinition)
        {
            methodInfo = (MethodInfo)MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, inner.TypeBuilder.TypeInfo.Type.TypeHandle)!;
        }

        return methodInfo;
    }
    
    private ArgumentException ThrowInvalidIndexer(string indexerExpressionName)
    {
        return new ArgumentException($"Argument {indexerExpressionName} must be valid indexer getter expression");
    }

    private IOptionalAliasBuilder<T> AddMemberInner(MemberInfo member)
    {
        return member switch
        {
            FieldInfo fld => AddField(fld),
            PropertyInfo prop => AddProperty(prop),
            _ => throw new ArgumentException($"Argument {nameof(member)} is not field or property")
        };
    }
    
    private ArgumentException ThrowInvalidMethodExpression(string methodExpressionName)
    {
        return new ArgumentException($"Argument {methodExpressionName} must be method call expression");
    }
}

internal class MemberBuilderSyntax(TypeBuilderFluentSyntax typeBuilder) : IOptionalAliasBuilder
{
    private Action<string>? _aliasAssignmentFn;
    private MemberBuilderSyntax? _next;
    public TypeBuilderFluentSyntax TypeBuilder { get; } = typeBuilder;

    public IMemberBuilder As(string alias)
    {
        _next = null;
        _aliasAssignmentFn!(alias);
        return new MemberBuilderSyntax(TypeBuilder);
    }

    public IOptionalAliasBuilder AddMethod(MethodInfo methodInfo)
    {
        if (TryForkSyntax(out var fork)) return fork!.AddMethod(methodInfo);
        
        ThrowIfOwnerTypeMismatch(methodInfo, TypeBuilder.TypeInfo.Type);
        var methods 
            = TypeBuilder.ModuleBuilder.ContainerBuilder.MethodInfoDict
                .GetValueOrDefault(TypeBuilder.TypeInfo);
        
        if (methods == null)
        {
            methods = [];
            TypeBuilder.ModuleBuilder.ContainerBuilder.MethodInfoDict.Add(TypeBuilder.TypeInfo, methods);
        }

        
        ThrowIfMemberNameExists(methodInfo.Name, methodInfo);
        DefaultMethodInfo? existingMethod;
        if ((existingMethod = methods.FirstOrDefault(x => x.MethodInfo == methodInfo)) == null)
        {
            existingMethod = new DefaultMethodInfo(TypeBuilder.TypeInfo, methodInfo, methodInfo.Name);
            methods.Add(existingMethod);
        }
        else
        {
            existingMethod.Alias = methodInfo.Name;
        }
        
        _aliasAssignmentFn = s =>
        {
            ThrowIfMemberNameExists(s, methodInfo);
            existingMethod.Alias = s;
        };

        _next = new MemberBuilderSyntax(TypeBuilder);
        return this;
    }

    public IMemberBuilder AddCtor(ConstructorInfo constructorInfo)
    {
        if (TryForkSyntax(out var fork)) return fork!.AddCtor(constructorInfo);
        
        ThrowIfOwnerTypeMismatch(constructorInfo, TypeBuilder.TypeInfo.Type);
        var constructors =
            TypeBuilder.ModuleBuilder.ContainerBuilder.CtorInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo);
        if (constructors == null)
        {
            constructors = [];
            TypeBuilder.ModuleBuilder.ContainerBuilder.CtorInfoDict.Add(TypeBuilder.TypeInfo, constructors);
        }

        if (constructors.FirstOrDefault(x => x.ConstructorInfo == constructorInfo) == null)
        {
            var existingCtor = new DefaultConstructorInfo(TypeBuilder.TypeInfo, constructorInfo);
            constructors.Add(existingCtor);
        }

        _next = new MemberBuilderSyntax(TypeBuilder);
        return this;
    }

    public IOptionalAliasBuilder AddField(FieldInfo fieldInfo)
    {
        if (TryForkSyntax(out var fork)) return fork!.AddField(fieldInfo);
        
        ThrowIfOwnerTypeMismatch(fieldInfo, TypeBuilder.TypeInfo.Type);

        var fields = TypeBuilder.ModuleBuilder.ContainerBuilder.FieldInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo);
        if (fields == null)
        {
            fields = [];
            TypeBuilder.ModuleBuilder.ContainerBuilder.FieldInfoDict.Add(TypeBuilder.TypeInfo, fields);
        }
        
        ThrowIfMemberNameExists(fieldInfo.Name, fieldInfo);
        DefaultFieldInfo? existingField;
        if ((existingField = fields.FirstOrDefault(x => x.FieldInfo == fieldInfo)) == null)
        {
            existingField = new DefaultFieldInfo(TypeBuilder.TypeInfo, fieldInfo.Name, fieldInfo);
            fields.Add(existingField);
        }
        else
        {
            existingField.Alias = fieldInfo.Name;
        }

        _aliasAssignmentFn = s =>
        {
            ThrowIfMemberNameExists(s, fieldInfo);
            existingField.Alias = s;
        };
        _next = new MemberBuilderSyntax(TypeBuilder);
        return this;
    }

    public IOptionalAliasBuilder AddProperty(PropertyInfo propertyInfo)
    {
        if (TryForkSyntax(out var fork)) return fork!.AddProperty(propertyInfo);
        
        ThrowIfOwnerTypeMismatch(propertyInfo, TypeBuilder.TypeInfo.Type);

        var props = TypeBuilder.ModuleBuilder.ContainerBuilder.PropInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo);
        if (props == null)
        {
            props = [];
            TypeBuilder.ModuleBuilder.ContainerBuilder.PropInfoDict.Add(TypeBuilder.TypeInfo, props);
        }

        ThrowIfMemberNameExists(propertyInfo.Name, propertyInfo);
        DefaultPropertyInfo? existingProps;
        if ((existingProps = props.FirstOrDefault(x => x.PropertyInfo == propertyInfo)) == null)
        {
            existingProps = new DefaultPropertyInfo(propertyInfo.Name, TypeBuilder.TypeInfo, propertyInfo);
            props.Add(existingProps);
        }
        else
        {
            existingProps.Alias = propertyInfo.Name;
        }

        _aliasAssignmentFn = s =>
        {
            ThrowIfMemberNameExists(s, propertyInfo);
            existingProps.Alias = s;
        };
        _next = new MemberBuilderSyntax(TypeBuilder);
        return this;
    }

    public IMemberBuilder AddIndexer(PropertyInfo propertyInfo)
    {
        if (TryForkSyntax(out var fork)) return fork!.AddIndexer(propertyInfo);
        
        ThrowIfOwnerTypeMismatch(propertyInfo, TypeBuilder.TypeInfo.Type);
        if (propertyInfo.GetIndexParameters().Length == 0) throw new ArgumentException("Invalid indexer property");
        
        var indexers =
            TypeBuilder.ModuleBuilder.ContainerBuilder.IndexerInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo);
        if (indexers == null)
        {
            indexers = [];
            TypeBuilder.ModuleBuilder.ContainerBuilder.IndexerInfoDict.Add(TypeBuilder.TypeInfo, indexers);
        }

        if (indexers.FirstOrDefault(x => x.IndexerProperty == propertyInfo) == null)
        {
            var existingIndexer = new DefaultIndexerInfo(TypeBuilder.TypeInfo, propertyInfo);
            indexers.Add(existingIndexer);
        }

        _next = new MemberBuilderSyntax(TypeBuilder);
        return this;
    }

    public IModuleBuilderSyntax CompleteType() => TypeBuilder.ModuleBuilder;

    //If alias wasn't set, but syntax in variable we guarantee that alias will apply to this member  
    private bool TryForkSyntax(out MemberBuilderSyntax? fork)
    {
        fork = null;
        if (_next == null) return false;
        
        fork = _next;
        _next = new MemberBuilderSyntax(TypeBuilder);
        return true;
    }
    
    private void ThrowIfMemberNameExists(string memberName, MemberInfo memberDefinition)
    {
        if (memberName == TypeBuilder.TypeInfo.Alias)
        {
            throw new ArgumentException($"Member cannot be named same as enclosing type {memberName}");
        }
        
        var fields = TypeBuilder.ModuleBuilder.ContainerBuilder.FieldInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo) ?? [];
        ThrowIfMemberExistsAmongList(fields, m => m.Alias, (m, i) => m.FieldInfo == i, memberName, memberDefinition, "Field");
        
        var properties = TypeBuilder.ModuleBuilder.ContainerBuilder.PropInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo) ?? [];
        ThrowIfMemberExistsAmongList(properties, m => m.Alias, (m, i) => m.PropertyInfo == i, memberName, memberDefinition, "Property");

        var methods = TypeBuilder.ModuleBuilder.ContainerBuilder.MethodInfoDict.GetValueOrDefault(TypeBuilder.TypeInfo) ?? [];
        if (memberDefinition is not MethodInfo methodInfo)
        {
            ThrowIfMemberExistsAmongList(methods, m => m.Alias, (m, i) => m.MethodInfo == i, memberName, memberDefinition, "Method");
        }
        else
        {
            CheckMatchMethods(methods, memberName, methodInfo);
        }
    }

    private void ThrowIfMemberExistsAmongList<TMember>(
        IEnumerable<TMember> members, 
        Func<TMember, string> aliasAccessor, 
        Func<TMember, MemberInfo, bool> infoComparer, 
        string memberName, 
        MemberInfo memberInfo,
        string memberTypeName)
    {
        foreach (var member in members)
        {
            if (aliasAccessor(member) == memberName && !infoComparer(member, memberInfo))
            {
                throw new ArgumentException(
                    $"{memberTypeName} {memberName} already exists in this type {TypeBuilder.TypeInfo.Alias}");
            }
        }
    }
    
    //TODO: directional modifiers(in, out, ref)
    private void CheckMatchMethods(List<DefaultMethodInfo> methodInfos, string alias, MethodInfo method)
    {
        var matchMethods = methodInfos
            .Where(x => x.MethodInfo != method && x.Alias == alias)
            .Select(x => x.MethodInfo);
        
        var parameterToMatch = method.GetParameters();
        foreach (var signature in matchMethods)
        {
            var parameters = signature.GetParameters();
            if (parameters.Length == parameterToMatch.Length
                && parameters.Zip(parameterToMatch)
                    .All(x => x.First.ParameterType == x.Second.ParameterType))
            {
                throw new ArgumentException($"Method with alias: {alias} already was defined in {TypeBuilder.TypeInfo.Alias}");
            }
        }
    }

    private void ThrowIfOwnerTypeMismatch(MemberInfo member, Type fromType)
    {
        var text = $"Member {member} does not belong to {fromType.Name}";
        if (!member.DeclaringType!.IsAssignableFrom(fromType))
        {
            throw new ArgumentException(text);
        }
        
        try
        {
            fromType.GetMemberWithSameMetadataDefinitionAs(member);
        }
        catch (Exception)
        {
            throw new ArgumentException(text);
        }
    }
}