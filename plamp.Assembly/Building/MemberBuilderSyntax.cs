using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Assembly.Building.Interfaces;
using plamp.Assembly.Models;

namespace plamp.Assembly.Building;

internal class MemberBuilderSyntax<T>(TypeBuilderFluentSyntax<T> typeBuilder) : IOptionalAliasBuilder<T>
{
    private Action<string>? _aliasAssignmentFn;
    
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

    public MethodInfo GetMethodOrGenericDefinition(MethodInfo methodInfo)
    {
        if (methodInfo.IsConstructedGenericMethod)
        {
            methodInfo = methodInfo.GetGenericMethodDefinition();
        }

        if (typeBuilder.TypeInfo.Type.IsGenericTypeDefinition)
        {
            methodInfo = (MethodInfo)MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, typeBuilder.TypeInfo.Type.TypeHandle)!;
        }

        return methodInfo;
    }

    public IOptionalAliasBuilder<T> AddMethod(MethodInfo methodInfo)
    {
        ThrowIfOwnerTypeMismatch(methodInfo, typeBuilder.TypeInfo.Type);
        var methods 
            = typeBuilder.ModuleBuilder.ContainerBuilder.MethodInfoDict
                .GetValueOrDefault(typeBuilder.TypeInfo);
        
        if (methods == null)
        {
            methods = [];
            typeBuilder.ModuleBuilder.ContainerBuilder.MethodInfoDict.Add(typeBuilder.TypeInfo, methods);
        }

        CheckMatchMethods(methods, methodInfo.Name, methodInfo);
        
        DefaultMethodInfo? existingMethod;
        if ((existingMethod = methods.FirstOrDefault(x => x.MethodInfo == methodInfo)) == null)
        {
            existingMethod = new DefaultMethodInfo(typeBuilder.TypeInfo, methodInfo, methodInfo.Name);
            methods.Add(existingMethod);
        }
        else
        {
            existingMethod.Alias = methodInfo.Name;
        }
        
        _aliasAssignmentFn = s =>
        {
            CheckMatchMethods(methods, s, methodInfo);
            existingMethod.Alias = s;
        };
        
        return this;

        void CheckMatchMethods(List<DefaultMethodInfo> methodInfos, string alias, MethodInfo method)
        {
            var matchMethods = methodInfos
                .Where(x => x.MethodInfo != method && x.Alias == alias)
                .Select(x => x.MethodInfo);
            ThrowIfSignatureMatches(matchMethods, method, alias);
        }
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
        
        var type = typeBuilder.TypeInfo.Type;
        if (type.IsGenericTypeDefinition)
        {
            ctorInfo = (ConstructorInfo)MethodBase.GetMethodFromHandle(ctorInfo.MethodHandle, type.TypeHandle)!;
        }

        return AddCtor(ctorInfo);
    }

    public IMemberBuilder<T> AddCtor(ConstructorInfo constructorInfo)
    {
        ThrowIfOwnerTypeMismatch(constructorInfo, typeBuilder.TypeInfo.Type);
        var constructors =
            typeBuilder.ModuleBuilder.ContainerBuilder.CtorInfoDict.GetValueOrDefault(typeBuilder.TypeInfo);
        if (constructors == null)
        {
            constructors = [];
            typeBuilder.ModuleBuilder.ContainerBuilder.CtorInfoDict.Add(typeBuilder.TypeInfo, constructors);
        }

        if (constructors.FirstOrDefault(x => x.ConstructorInfo == constructorInfo) == null)
        {
            var existingCtor = new DefaultConstructorInfo(typeBuilder.TypeInfo, constructorInfo);
            constructors.Add(existingCtor);
        }

        return this;
    }

    public IOptionalAliasBuilder<T> AddMember(Expression<Func<T, object>> memberExpression)
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

        var type = typeBuilder.TypeInfo.Type;
        if (type.IsGenericTypeDefinition)
        {
            //Fld or prop
            memberInfo = type.GetMember(memberInfo.Name).First();
        }
        
        return AddMemberInner(memberInfo);
    }

    public IOptionalAliasBuilder<T> AddField(FieldInfo fieldInfo)
    {
        ThrowIfOwnerTypeMismatch(fieldInfo, typeBuilder.TypeInfo.Type);

        var fields = typeBuilder.ModuleBuilder.ContainerBuilder.FieldInfoDict.GetValueOrDefault(typeBuilder.TypeInfo);
        if (fields == null)
        {
            fields = [];
            typeBuilder.ModuleBuilder.ContainerBuilder.FieldInfoDict.Add(typeBuilder.TypeInfo, fields);
        }

        DefaultFieldInfo? existingField;
        if ((existingField = fields.FirstOrDefault(x => x.FieldInfo == fieldInfo)) == null)
        {
            existingField = new DefaultFieldInfo(typeBuilder.TypeInfo, fieldInfo.Name, fieldInfo);
            fields.Add(existingField);
        }
        else
        {
            existingField.Alias = fieldInfo.Name;
        }

        _aliasAssignmentFn = s => existingField.Alias = s;
        return this;
    }

    public IOptionalAliasBuilder<T> AddProperty(PropertyInfo propertyInfo)
    {
        ThrowIfOwnerTypeMismatch(propertyInfo, typeBuilder.TypeInfo.Type);

        var props = typeBuilder.ModuleBuilder.ContainerBuilder.PropInfoDict.GetValueOrDefault(typeBuilder.TypeInfo);
        if (props == null)
        {
            props = [];
            typeBuilder.ModuleBuilder.ContainerBuilder.PropInfoDict.Add(typeBuilder.TypeInfo, props);
        }

        DefaultPropertyInfo? existingProps;
        if ((existingProps = props.FirstOrDefault(x => x.PropertyInfo == propertyInfo)) == null)
        {
            existingProps = new DefaultPropertyInfo(propertyInfo.Name, typeBuilder.TypeInfo, propertyInfo);
            props.Add(existingProps);
        }
        else
        {
            existingProps.Alias = propertyInfo.Name;
        }

        _aliasAssignmentFn = s => existingProps.Alias = s;
        return this;
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

        var type = typeBuilder.TypeInfo.Type;
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
        ThrowIfOwnerTypeMismatch(propertyInfo, typeBuilder.TypeInfo.Type);
        if (propertyInfo.GetIndexParameters().Length == 0) throw new ArgumentException("Invalid indexer property");
        
        var indexers =
            typeBuilder.ModuleBuilder.ContainerBuilder.IndexerInfoDict.GetValueOrDefault(typeBuilder.TypeInfo);
        if (indexers == null)
        {
            indexers = [];
            typeBuilder.ModuleBuilder.ContainerBuilder.IndexerInfoDict.Add(typeBuilder.TypeInfo, indexers);
        }

        if (indexers.FirstOrDefault(x => x.IndexerProperty == propertyInfo) == null)
        {
            var existingIndexer = new DefaultIndexerInfo(typeBuilder.TypeInfo, propertyInfo);
            indexers.Add(existingIndexer);
        }

        return this;
    }

    public IModuleBuilderSyntax CompleteType() => typeBuilder.ModuleBuilder;

    public IMemberBuilder<T> As(string alias)
    {
        _aliasAssignmentFn!(alias);
        return this;
    }

    //TODO: directional modifiers(in, out, ref)
    private void ThrowIfSignatureMatches(IEnumerable<MethodInfo> signatures, MethodInfo toMatch, string alias)
    {
        var parameterToMatch = toMatch.GetParameters();
        foreach (var signature in signatures)
        {
            var parameters = signature.GetParameters();
            if (parameters.Length == parameterToMatch.Length
                && parameters.Zip(parameterToMatch)
                    .All(x => x.First.ParameterType == x.Second.ParameterType))
            {
                throw new ArgumentException($"Method with alias: {alias} already was defined in {typeBuilder.TypeInfo.Alias}");
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