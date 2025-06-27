using System;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class MemberCollisionTests
{
    #region Field
    
    [Fact]
    public void FieldAliasPropCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField).As(nameof(ExampleType.ExampleProp));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.ExampleProp));
    }
    
    [Fact]
    public void FieldPropAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField)
            .AddPropertyOrField(x => x.ExampleProp);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleField)));
    }
    
    [Fact]
    public void FieldPropFieldAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField);
        syntax.AddPropertyOrField(x => x.ExampleProp);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleProp)));
    }
    
    [Fact]
    public void FieldAliasMethodCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField).As(nameof(ExampleType.ExampleMethod));
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(x => x.ExampleMethod()));
    }
    
    [Fact]
    public void FieldMethodAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField)
            .AddMethod(x => x.ExampleMethod());
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleField)));
    }
    
    [Fact]
    public void FieldMethodFieldAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField);
        syntax.AddMethod(x => x.ExampleMethod());
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleMethod)));
    }

    #endregion

    #region Method

    [Fact]
    public void MethodAliasPropCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod()).As(nameof(ExampleType.ExampleProp));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.ExampleProp));
    }
    
    [Fact]
    public void MethodPropAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod())
            .AddPropertyOrField(x => x.ExampleProp);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleMethod)));
    }
    
    [Fact]
    public void MethodPropFieldAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod());
        syntax.AddPropertyOrField(x => x.ExampleProp);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleProp)));
    }
    
    [Fact]
    public void MethodAliasFieldCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod()).As(nameof(ExampleType.ExampleField));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.ExampleField));
    }
    
    [Fact]
    public void MethodFieldAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod())
            .AddPropertyOrField(x => x.ExampleField);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleMethod)));
    }
    
    [Fact]
    public void MethodFieldMethodAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod());
        syntax.AddPropertyOrField(x => x.ExampleField);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleField)));
    }

    #endregion

    #region Property

    [Fact]
    public void PropertyAliasFieldCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp).As(nameof(ExampleType.ExampleField));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.ExampleField));
    }
    
    [Fact]
    public void PropertyFieldAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp)
            .AddPropertyOrField(x => x.ExampleField);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleProp)));
    }
    
    [Fact]
    public void PropertyFieldPropertyAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp);
        syntax.AddPropertyOrField(x => x.ExampleField);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleField)));
    }
    
    [Fact]
    public void PropertyAliasMethodCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp).As(nameof(ExampleType.ExampleMethod));
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(x => x.ExampleMethod()));
    }
    
    [Fact]
    public void PropertyMethodAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp)
            .AddMethod(x => x.ExampleMethod());
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleProp)));
    }
    
    [Fact]
    public void PropertyMethodPropertyAliasCollision()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp);
        syntax.AddMethod(x => x.ExampleMethod());
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleMethod)));
    }

    #endregion

    #region TypeCollision

    [Fact]
    public void TypeFieldAlias()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleField);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType)));
    }

    [Fact]
    public void TypeAliasField()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().As(nameof(ExampleType.ExampleField));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.ExampleField));
    }

    [Fact]
    public void TypeFieldTypeAlias()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>();
        syntax.WithMembers().AddPropertyOrField(x => x.ExampleField);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleField)));
    }
    
    [Fact]
    public void TypePropAlias()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddPropertyOrField(x => x.ExampleProp);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType)));
    }

    [Fact]
    public void TypeAliasProp()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().As(nameof(ExampleType.ExampleProp));
        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.ExampleProp));
    }

    [Fact]
    public void TypePropTypeAlias()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>();
        syntax.WithMembers().AddPropertyOrField(x => x.ExampleProp);
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleProp)));
    }
    
    [Fact]
    public void TypeMethodAlias()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().WithMembers()
            .AddMethod(x => x.ExampleMethod());
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType)));
    }

    [Fact]
    public void TypeAliasMethod()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>().As(nameof(ExampleType.ExampleMethod));
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(x => x.ExampleMethod()));
    }

    [Fact]
    public void TypeMethodTypeAlias()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleType>();
        syntax.WithMembers().AddMethod(x => x.ExampleMethod());
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleType.ExampleMethod)));
    }

    #endregion
}

public class ExampleType
{
    public int ExampleField;

    public int ExampleMethod() => 0;
    
    public int ExampleProp { get; set; }
}