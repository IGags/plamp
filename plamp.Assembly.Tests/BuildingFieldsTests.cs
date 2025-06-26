using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingFieldsTests
{
    [Fact]
    public void AddSimpleFieldInfo()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var fldInfo = typeof(ExampleFieldClass<int>).GetField(nameof(ExampleFieldClass<int>.SimpleFiled))!;
        builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers().AddField(fldInfo);
        var container = builder.CreateContainer();
        
        var types = container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1);
        var type = Assert.Single(types);
        var fields = container.GetMatchingFields(nameof(ExampleFieldClass<int>.SimpleFiled), type);
        var field = Assert.Single(fields);
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(fldInfo, field.FieldInfo);
        Assert.Equal(nameof(ExampleFieldClass<int>.SimpleFiled), field.Alias);
    }

    [Fact]
    public void AddSimpleFieldExpression()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers().AddPropertyOrField(x => x.SimpleFiled);
        
        var container = builder.CreateContainer();
        var fldInfo = typeof(ExampleFieldClass<int>).GetField(nameof(ExampleFieldClass<int>.SimpleFiled))!;
        var type = Assert.Single(container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1));
        var fields = container.GetMatchingFields(nameof(ExampleFieldClass<int>.SimpleFiled), type);
        var field = Assert.Single(fields);
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(fldInfo, field.FieldInfo);
        Assert.Equal(nameof(ExampleFieldClass<int>.SimpleFiled), field.Alias);
    }

    [Fact]
    public void AddFieldWithAlias()
    {
        const string alias = "complexField";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleFiled).As(alias);
        var container = builder.CreateContainer();
        var fldInfo = typeof(ExampleFieldClass<int>).GetField(nameof(ExampleFieldClass<int>.SimpleFiled))!;
        
        var type =  Assert.Single(container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1));
        var field = Assert.Single(container.GetMatchingFields(alias, type));
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(alias, field.Alias);
        Assert.Equal(fldInfo, field.FieldInfo);
    }

    [Fact]
    public void AddFieldTwiceRewriteAlias()
    {
        const string alias = "complexField";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleFiled).As(alias)
            .AddPropertyOrField(x => x.SimpleFiled);
        
        var fldInfo = typeof(ExampleFieldClass<int>).GetField(nameof(ExampleFieldClass<int>.SimpleFiled));
        var container = builder.CreateContainer();
        
        var type =  Assert.Single(container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1));
        var field = Assert.Single(container.GetMatchingFields(nameof(ExampleFieldClass<int>.SimpleFiled), type));
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(nameof(ExampleFieldClass<int>.SimpleFiled), field.Alias);
        Assert.Equal(fldInfo, field.FieldInfo);
    }
    
    [Fact]
    public void AddFieldTwiceAddAlias()
    {
        const string alias = "complexField";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleFiled)
            .AddPropertyOrField(x => x.SimpleFiled).As(alias);
        
        var fldInfo = typeof(ExampleFieldClass<int>).GetField(nameof(ExampleFieldClass<int>.SimpleFiled));
        var container = builder.CreateContainer();
        
        var type =  Assert.Single(container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1));
        var field = Assert.Single(container.GetMatchingFields(alias, type));
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(alias, field.Alias);
        Assert.Equal(fldInfo, field.FieldInfo);
    }

    [Fact]
    public void AddGenericFieldImpl()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<ExampleFieldClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.GenericField);
        var container = builder.CreateContainer();
        var fldInfo = typeof(ExampleFieldClass<int>).GetField(nameof(ExampleFieldClass<int>.GenericField));
        var type =  Assert.Single(container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1));
        var fields = container.GetMatchingFields(nameof(ExampleFieldClass<int>.GenericField), type);
        var field = Assert.Single(fields);
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(fldInfo, field.FieldInfo);
        Assert.Equal(nameof(ExampleFieldClass<int>.GenericField), field.Alias);
    }

    [Fact]
    public void AddGenericFieldDefinition()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddGenericTypeDefinition<ExampleFieldClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.GenericField);
        var container = builder.CreateContainer();
        
        var fldInfo = typeof(ExampleFieldClass<int>).GetGenericTypeDefinition().GetField(nameof(ExampleFieldClass<int>.GenericField));
        var type =  Assert.Single(container.GetMatchingTypes(nameof(ExampleFieldClass<int>), 1));
        var fields = container.GetMatchingFields(nameof(ExampleFieldClass<int>.GenericField), type);
        var field = Assert.Single(fields);
        Assert.Equal(type, field.EnclosingType);
        Assert.Equal(fldInfo, field.FieldInfo);
        Assert.Equal(nameof(ExampleFieldClass<int>.GenericField), field.Alias);
    }

    [Fact]
    public void AddFieldFromOtherType()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var otherFld = typeof(OtherFieldClass).GetField(nameof(OtherFieldClass.SimpleFiled))!;
        var syntax = builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers();
        Assert.Throws<ArgumentException>(() => syntax.AddField(otherFld));
    }

    [Fact]
    public void AddTwoFieldsWithSameAlias()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleFieldClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleFiled).As(nameof(ExampleFieldClass<int>.GenericField));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.GenericField));
    }
}

public class ExampleFieldClass<T>
{
    public int SimpleFiled;

    public T GenericField = default!;
}

public class OtherFieldClass
{
    public int SimpleFiled;
}
