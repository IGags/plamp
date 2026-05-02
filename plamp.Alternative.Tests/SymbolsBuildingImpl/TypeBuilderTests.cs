using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;
using TypeBuilder = plamp.Alternative.SymbolsBuildingImpl.TypeBuilder;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class TypeBuilderTests
{
    private const string ModuleName = "ttt";
    
    [Fact]
    public void CreateSimpleType_Correct()
    {

        var type = new TypeBuilder("ee", ModuleName);
        type.ModuleName.ShouldBe(ModuleName);
        type.Name.ShouldBe("ee");
        type.DefinitionName.ShouldBe("ee");
        type.IsGenericTypeDefinition.ShouldBeFalse();
    }

    [Fact]
    public void CreateTypeWithGenericParam_Correct()
    {
        var paramBuilder = new GenericParameterBuilder("T", ModuleName);

        var type = new TypeBuilder("abc", [paramBuilder], ModuleName);
        type.Name.ShouldBe("abc[T]");
        type.DefinitionName.ShouldBe("abc");
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    public void CreateTypeWithTwoParams_Correct()
    {
        var paramBuilder1 = new GenericParameterBuilder("T", ModuleName);
        var paramBuilder2 = new GenericParameterBuilder("T2", ModuleName);
        
        var type = new TypeBuilder("abc", [paramBuilder1, paramBuilder2], ModuleName);
        type.Name.ShouldBe("abc[T, T2]");
        type.DefinitionName.ShouldBe("abc");
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    public void CreateTypeWithTwoSameParams_Throws()
    {
        var paramBuilder1 = new GenericParameterBuilder("T", ModuleName);
        var paramBuilder2 = new GenericParameterBuilder("T", ModuleName);
        
        Should.Throw<InvalidOperationException>(() => new TypeBuilder("abc", [paramBuilder1, paramBuilder2], ModuleName));
    }

    [Fact]
    public void MakeArrayFromGenericDef_Throws()
    {
        var paramBuilder = new GenericParameterBuilder("T", ModuleName);
        
        var type = new TypeBuilder("abc", [paramBuilder], ModuleName);
        Should.Throw<InvalidOperationException>(() => type.MakeArrayType());
    }

    [Fact]
    public void ElementTypeReturnsNull_Correct()
    {
        var type = new TypeBuilder("abc", ModuleName);
        type.ElementType().ShouldBeNull();
    }

    [Fact]
    public void GenericDefFromGenericNull_Correct()
    {
        var paramBuilder = new GenericParameterBuilder("T", ModuleName);

        var type = new TypeBuilder("ff", [paramBuilder], ModuleName);
        type.GetGenericTypeDefinition().ShouldBeNull();
    }

    [Fact]
    public void GenericArguments_ShouldBeEmpty()
    {
        var paramBuilder = new GenericParameterBuilder("T", ModuleName);

        var type = new TypeBuilder("ff", [paramBuilder], ModuleName);
        type.GetGenericArguments().ShouldBeEmpty();
    }

    [Fact]
    public void GetGenericParamsFromGeneric_ReturnsTypeCollection()
    {
        var paramBuilder = new GenericParameterBuilder("T", ModuleName);
        
        var type = new TypeBuilder("ff", [paramBuilder], ModuleName);
        var collection = type.GetGenericParameters();
        var paramType = collection.ShouldHaveSingleItem();

        paramType.IsGenericTypeParameter.ShouldBeTrue();
        paramType.DefinitionName.ShouldBe("T");
        paramType.Name.ShouldBe("T");
    }

    [Fact]
    public void AsTypeWithoutType_Throws()
    {
        var type = new TypeBuilder("ff", ModuleName);
        Should.Throw<InvalidOperationException>(() => type.AsType());
    }

    [Fact]
    public void AsTypeWithBuilder_ReturnsBuilder()
    {
        var type = new TypeBuilder("f1", ModuleName);
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule("Module");
        var typ = module.DefineType("f1");
        type.Type = typ;
        
        type.AsType().ShouldBe(typ);
    }

    [Fact]
    public void DefineField_Correct()
    {
        var type = new TypeBuilder("ff", ModuleName);
        var fldType = new TypeBuilder("fff", ModuleName);
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType },
            new FieldNameNode("a"));
        
        type.AddField(fieldNode);
        var fld = type.Fields.ShouldHaveSingleItem();
        fld.FieldType.ShouldBe(fldType);
        fld.Name.ShouldBe("a");
    }

    [Fact]
    public void DefineFieldWithoutType_Throws()
    {
        var type = new TypeBuilder("ff", ModuleName);
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")),
            new FieldNameNode("a"));
        
        Should.Throw<InvalidOperationException>(() => type.AddField(fieldNode));
    }

    [Fact]
    public void DefineDuplicateFieldName_Throws()
    {
        var type = new TypeBuilder("ff", ModuleName);
        
        var fldType1 = new TypeBuilder("fff", ModuleName);
        var fieldNode1 = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType1 },
            new FieldNameNode("a"));
        
        var fldType2 = new TypeBuilder("ffff", ModuleName);
        var fieldNode2 = new FieldDefNode(
            new TypeNode(new TypeNameNode("ffff")) { TypeInfo = fldType2 },
            new FieldNameNode("a"));
        
        type.AddField(fieldNode1);
        Should.Throw<InvalidOperationException>(() => type.AddField(fieldNode2));
    }

    [Fact]
    public void EqualityByNameAndModule_Correct()
    {
        var type1 = new TypeBuilder("ff", ModuleName);
        var type2 = new TypeBuilder("ff", ModuleName);
        
        var fldType = new TypeBuilder("fff", "OtherMod");
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType },
            new FieldNameNode("a"));
        type1.AddField(fieldNode);
        
        type1.ShouldBe(type2);
    }

    [Fact]
    public void HashByNameAndModule_Correct()
    {
        var type1 = new TypeBuilder("ff", ModuleName);
        var type2 = new TypeBuilder("ff", ModuleName);
        
        var fldType = new TypeBuilder("fff", "OtherMod");
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType },
            new FieldNameNode("a"));
        type1.AddField(fieldNode);
        
        type1.GetHashCode().ShouldBe(type2.GetHashCode());
    }

    [Fact]
    public void MakeGenericFromNonGeneric_ReturnsNull()
    {
        var type = new TypeBuilder("f", ModuleName);
        var res = type.MakeGenericType([new TypeBuilder("123", ModuleName)]);
        res.ShouldBeNull();
    }
}