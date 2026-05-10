using System.Collections.Generic;
using System;
using plamp.Alternative.SymbolsImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsImpl;

public class TypeInfoTests
{
    public const string ModuleName = "test";
    
    [Fact]
    public void MakeFromType_Correct()
    {
        var type = typeof(string);
        var info = TypeInfo.FromType(type, ModuleName);

        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe(type.Name);
        info.DefinitionName.ShouldBe(type.Name);
        info.IsGenericType.ShouldBeFalse();
        info.IsArrayType.ShouldBeFalse();
        info.IsGenericTypeDefinition.ShouldBeFalse();
        info.IsGenericTypeParameter.ShouldBeFalse();
        info.Fields.ShouldBeEmpty();
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeWithOverride_Correct()
    {
        var type = typeof(string);
        const string nameOverride = "text";
        var info = TypeInfo.FromType(type, ModuleName, nameOverride);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe(nameOverride);
        info.DefinitionName.ShouldBe(nameOverride);
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeFromArrayType_Correct()
    {
        var type = typeof(string[]);
        var info = TypeInfo.FromType(type, ModuleName);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("[]String");
        info.DefinitionName.ShouldBe("String");
        type.IsArray.ShouldBeTrue();
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeFromArrayTypeWithOverride_Correct()
    {
        var type = typeof(string[]);
        const string nameOverride = "text";
        var info = TypeInfo.FromType(type, ModuleName, nameOverride);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("[]text");
        info.DefinitionName.ShouldBe(nameOverride);
        type.IsArray.ShouldBeTrue();
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeFromGenericTypeDef_Correct()
    {
        var type = typeof(KeyValuePair<,>);
        var info = TypeInfo.FromType(type, ModuleName);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("KeyValuePair[TKey, TValue]");
        info.DefinitionName.ShouldBe("KeyValuePair");
        info.IsGenericType.ShouldBeFalse();
        info.IsGenericTypeDefinition.ShouldBeTrue();
        info.IsGenericTypeParameter.ShouldBeFalse();
        info.IsArrayType.ShouldBeFalse();
        
        info.AsType().ShouldBe(type);
    }
    
    [Fact]
    public void MakeFromGenericTypeDefWithOverride_Correct()
    {
        var type = typeof(KeyValuePair<,>);
        const string nameOverride = "Pair";
        var info = TypeInfo.FromType(type, ModuleName, nameOverride);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("Pair[TKey, TValue]");
        info.DefinitionName.ShouldBe(nameOverride);
        
        info.AsType().ShouldBe(type);
    }

    //TODO: Типы аргументов не подставляют оверрайды имён.
    [Fact]
    public void MakeTypeFromGenericImpl_Correct()
    {
        var type = typeof(KeyValuePair<string, string>);
        var info = TypeInfo.FromType(type, ModuleName);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("KeyValuePair[String, String]");
        info.DefinitionName.ShouldBe("KeyValuePair");
        info.IsGenericType.ShouldBeTrue();
        info.IsGenericTypeDefinition.ShouldBeFalse();
        info.IsGenericTypeParameter.ShouldBeFalse();
        info.IsArrayType.ShouldBeFalse();
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeTypeFromGenericImplWithOverride_Correct()
    {
        var type = typeof(KeyValuePair<string, string>);
        const string nameOverride = "Pair";
        var info = TypeInfo.FromType(type, ModuleName, nameOverride);
        
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("Pair[String, String]");
        info.DefinitionName.ShouldBe(nameOverride);
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeTypeFromGenericTypeArray_Correct()
    {
        var type = typeof(KeyValuePair<,>).MakeArrayType();
        var info = TypeInfo.FromType(type, ModuleName);
        info.ShouldNotBeNull();
        info.ModuleName.ShouldBe(ModuleName);
        info.Name.ShouldBe("[]KeyValuePair[TKey, TValue]");
        info.DefinitionName.ShouldBe("KeyValuePair");
        info.IsGenericType.ShouldBeFalse();
        info.IsGenericTypeDefinition.ShouldBeFalse();
        info.IsGenericTypeParameter.ShouldBeFalse();
        info.IsArrayType.ShouldBeTrue();
        
        info.AsType().ShouldBe(type);
    }

    [Fact]
    public void MakeFromTypeWithEmptyModuleName_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => TypeInfo.FromType(typeof(string), ""));
    }
}
