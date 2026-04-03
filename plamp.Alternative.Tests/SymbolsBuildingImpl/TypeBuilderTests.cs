using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.ComplexTypes;
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
    [Fact]
    public void CreateSimpleType_Correct()
    {
        const string modName = "ttt";
        var tb = new SymTableBuilder()
        {
            ModuleName = modName
        };

        var type = new TypeBuilder("ee", tb);
        type.ModuleName.ShouldBe(modName);
        type.Name.ShouldBe("ee");
        type.DefinitionName.ShouldBe("ee");
        type.IsGenericTypeDefinition.ShouldBeFalse();
    }

    [Fact]
    public void CreateTypeWithGenericParam_Correct()
    {
        var genericName = new GenericParameterNameNode("T");
        var param = new GenericDefinitionNode(genericName);

        var type = new TypeBuilder("abc", [param], new());
        type.Name.ShouldBe("abc[T]");
        type.DefinitionName.ShouldBe("abc");
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    public void CreateTypeWithTwoParams_Correct()
    {
        var genericName1 = new GenericParameterNameNode("T");
        var genericName2 = new GenericParameterNameNode("T2");
        var param1 = new GenericDefinitionNode(genericName1);
        var param2 = new GenericDefinitionNode(genericName2);
        
        var type = new TypeBuilder("abc", [param1, param2], new());
        type.Name.ShouldBe("abc[T, T2]");
        type.DefinitionName.ShouldBe("abc");
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    public void CreateTypeWithTwoSameParams_Throws()
    {
        var genericName1 = new GenericParameterNameNode("T");
        var genericName2 = new GenericParameterNameNode("T");
        var param1 = new GenericDefinitionNode(genericName1);
        var param2 = new GenericDefinitionNode(genericName2);
        
        Should.Throw<InvalidOperationException>(() => new TypeBuilder("abc", [param1, param2], new()));
    }

    [Fact]
    public void MakeArrayFromGenericDef_Throws()
    {
        var genericName = new GenericParameterNameNode("T");
        var param = new GenericDefinitionNode(genericName);
        
        var type = new TypeBuilder("abc", [param], new());
        Should.Throw<InvalidOperationException>(() => type.MakeArrayType());
    }

    [Fact]
    public void ElementTypeReturnsNull_Correct()
    {
        var type = new TypeBuilder("abc", new());
        type.ElementType().ShouldBeNull();
    }

    [Fact]
    public void GenericDefFromGenericNull_Correct()
    {
        var genericName = new GenericParameterNameNode("T");
        var param = new GenericDefinitionNode(genericName);

        var type = new TypeBuilder("ff", [param], new());
        type.GetGenericTypeDefinition().ShouldBeNull();
    }

    [Fact]
    public void GenericArguments_ShouldBeEmpty()
    {
        var genericName = new GenericParameterNameNode("T");
        var param = new GenericDefinitionNode(genericName);

        var type = new TypeBuilder("ff", [param], new());
        type.GetGenericArguments().ShouldBeEmpty();
    }

    [Fact]
    public void GetGenericParamsFromGeneric_ReturnsTypeCollection()
    {
        var genericName = new GenericParameterNameNode("T");
        var param = new GenericDefinitionNode(genericName);
        
        var type = new TypeBuilder("ff", [param], new());
        var collection = type.GetGenericParameters();
        var paramType = collection.ShouldHaveSingleItem();

        paramType.IsGenericTypeParameter.ShouldBeTrue();
        paramType.DefinitionName.ShouldBe("T");
        paramType.Name.ShouldBe("T");
    }

    [Fact]
    public void AsTypeWithoutType_Throws()
    {
        var type = new TypeBuilder("ff", new());
        Should.Throw<InvalidOperationException>(() => type.AsType());
    }

    [Fact]
    public void AsTypeWithBuilder_ReturnsBuilder()
    {
        var type = new TypeBuilder("f1", new SymTableBuilder());
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule("Module");
        var typ = module.DefineType("f1");
        type.Type = typ;
        
        type.AsType().ShouldBe(typ);
    }

    [Fact]
    public void DefineField_Correct()
    {
        var type = new TypeBuilder("ff", new());
        var fldType = new TypeBuilder("fff", new ());
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
        var type = new TypeBuilder("ff", new());
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")),
            new FieldNameNode("a"));
        
        Should.Throw<InvalidOperationException>(() => type.AddField(fieldNode));
    }

    [Fact]
    public void DefineDuplicateFieldName_Throws()
    {
        var type = new TypeBuilder("ff", new());
        
        var fldType1 = new TypeBuilder("fff", new ());
        var fieldNode1 = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType1 },
            new FieldNameNode("a"));
        
        var fldType2 = new TypeBuilder("ffff", new ());
        var fieldNode2 = new FieldDefNode(
            new TypeNode(new TypeNameNode("ffff")) { TypeInfo = fldType2 },
            new FieldNameNode("a"));
        
        type.AddField(fieldNode1);
        Should.Throw<InvalidOperationException>(() => type.AddField(fieldNode2));
    }

    [Fact]
    public void EqualityByNameAndModule_Correct()
    {
        var type1 = new TypeBuilder("ff", new() { ModuleName = "ModD" });
        var type2 = new TypeBuilder("ff", new() { ModuleName = "ModD" });
        
        var fldType = new TypeBuilder("fff", new ());
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType },
            new FieldNameNode("a"));
        type1.AddField(fieldNode);
        
        type1.ShouldBe(type2);
    }

    [Fact]
    public void HashByNameAndModule_Correct()
    {
        var type1 = new TypeBuilder("ff", new() { ModuleName = "ModD" });
        var type2 = new TypeBuilder("ff", new() { ModuleName = "ModD" });
        
        var fldType = new TypeBuilder("fff", new ());
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("fff")) { TypeInfo = fldType },
            new FieldNameNode("a"));
        type1.AddField(fieldNode);
        
        type1.GetHashCode().ShouldBe(type2.GetHashCode());
    }

    [Fact]
    public void MakeGenericFromNonGeneric_ReturnsNull()
    {
        
    }
}