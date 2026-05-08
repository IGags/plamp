using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;
using TypeBuilder = plamp.Alternative.SymbolsBuildingImpl.TypeBuilder;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class ArrayTypeBuilderTests
{
    private const string OrigTypeName = "testType";
    
    private const string ModName = "testMod";
    
    [Fact]
    public void ArrayTypeOverall_Correct()
    {
        var instance = CreateInstance();
        instance.Name.ShouldBe("[]" + OrigTypeName);
        instance.DefinitionName.ShouldBe(OrigTypeName);
        instance.ModuleName.ShouldBe(ModName);
        instance.Fields.ShouldBeEmpty();
        instance.IsArrayType.ShouldBeTrue();
        instance.IsGenericType.ShouldBeFalse();
        instance.IsGenericTypeDefinition.ShouldBeFalse();
        instance.IsGenericTypeParameter.ShouldBeFalse();
        instance.GetGenericParameters().ShouldBeEmpty();
        instance.GetGenericArguments().ShouldBeEmpty();
        instance.GetGenericTypeDefinition().ShouldBeNull();
    }

    [Fact]
    public void GetElementType_Correct()
    {
        var elemType = new TypeBuilder("ABC", ModName);
        
        var arrType = elemType.MakeArrayType();
        arrType.ElementType().ShouldBe(elemType);
    }

    [Fact]
    public void AsType_ReturnsArrayOfElement()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule("Module");
        var type = module.DefineType(OrigTypeName, TypeAttributes.Public);

        var builder = new TypeBuilder(OrigTypeName, ModName) { Type = type };
        var array = builder.MakeArrayType();
        
        var asType = array.AsType();
        var elem = asType.GetElementType();
        //У .net есть такой прикол, что тип массива будет принадлежать той сборке, которая сделала MakeArrayType
        elem.ShouldBe(type);
    }

    [Fact]
    public void EqualityElemRoot_Correct()
    {
        var instance = CreateInstance();
        var elem = instance.ElementType();
        instance.ShouldNotBe(elem);
        instance.ShouldNotBe(instance.MakeArrayType());
        instance.ShouldBe(elem.MakeArrayType());
    }

    [Fact]
    public void MakeArrayFromVoid_Throws()
    {
        var baseType = Builtins.Void;
        Should.Throw<InvalidOperationException>(() => baseType.MakeArrayType());
    }
    
    private ArrayTypeBuilder CreateInstance()
    {
        return (ArrayTypeBuilder)new TypeBuilder(OrigTypeName, ModName).MakeArrayType();
    }
}