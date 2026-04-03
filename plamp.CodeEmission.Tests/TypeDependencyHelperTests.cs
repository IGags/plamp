using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.ILCodeEmitters;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class TypeDependencyHelperTests
{
    [Fact]
    public void GetFieldDepsSimple_Correct()
    {
        var builder = new SymTableBuilder();
        
        const string name1 = "A";
        var node1 = new TypedefNode(new TypedefNameNode(name1), [], []);
        var type = builder.DefineType(node1);

        var fldType = new TypeNode(new TypeNameNode(name1)) { TypeInfo = type };
        var fld = new FieldDefNode(fldType, new FieldNameNode("a"));
        var node2 = new TypedefNode(new TypedefNameNode("B"), [fld], []);
        
        var type2 = builder.DefineType(node2);
        type2.AddField(fld);

        var deps = TypeDependencyHelper.GetFieldDeps(type, type2);
        var dep = deps.ShouldHaveSingleItem();
        dep.ShouldBe(type);
    }

    [Fact]
    public void GetFieldDeps_FieldIsGenericTypeParam_Empty()
    {
        var builder = new SymTableBuilder();

        var fldType = new TypeNode(new TypeNameNode("T"));
        var fld = new FieldDefNode(fldType, new FieldNameNode("a"));
        var generic = new GenericDefinitionNode(new GenericParameterNameNode("T"));
        var node = new TypedefNode(new TypedefNameNode("A"), [fld], [generic]);
        var type = builder.DefineType(node, [generic]);
        var genericInfo = fldType.TypeInfo = type.GetGenericParameters().Single();
        
        type.AddField(fld);
        
        var deps = TypeDependencyHelper.GetFieldDeps(genericInfo, type);
        deps.ShouldBeEmpty();
    }

    [Fact]
    public void GetFieldDeps_FieldIsGenericTypeDef_Throws()
    {
        var builder = new SymTableBuilder();

        var gen1 = new GenericDefinitionNode(new GenericParameterNameNode("T"));
        var typeNode1 = new TypedefNode(new TypedefNameNode("A"), [], [gen1]);
        var type1 = builder.DefineType(typeNode1, [gen1]);

        var fldType = new TypeNode(new TypeNameNode("A[T]")) { TypeInfo = type1 };
        var fld = new FieldDefNode(fldType, new FieldNameNode("a"));
        var typeNode2 = new TypedefNode(new TypedefNameNode("B"), [fld], []);
        
        var type2 = builder.DefineType(typeNode2);
        type2.AddField(fld);
        
        Should.Throw<Exception>(() => TypeDependencyHelper.GetFieldDeps(type1, type2));
    }

    [Fact]
    public void GetFieldDeps_UnwrapArrayField_Correct()
    {
        var builder = new SymTableBuilder();

        var typeNode1 = new TypedefNode(new TypedefNameNode("A"), [], []);
        var type1 = builder.DefineType(typeNode1);

        var arrType = type1.MakeArrayType();
        var fldType = new TypeNode(new TypeNameNode("[]A")) { TypeInfo = type1 };
        var fld = new FieldDefNode(fldType, new FieldNameNode("a"));

        var typeNode2 = new TypedefNode(new TypedefNameNode("B"), [fld], []);
        
        var type2 = builder.DefineType(typeNode2);
        type2.AddField(fld);
        
        var deps = TypeDependencyHelper.GetFieldDeps(arrType, type2);
        var dep = deps.ShouldHaveSingleItem();
        dep.ShouldBe(type1);
    }

    [Fact]
    public void GetFieldDeps_FieldHasSameTypeAsDefining_ReturnsEmpty()
    {
        var builder = new SymTableBuilder();

        var fldType = new TypeNode(new TypeNameNode("A"));
        var fld = new FieldDefNode(fldType, new FieldNameNode("a"));
        
        var typeNode1 = new TypedefNode(new TypedefNameNode("A"), [fld], []);
        var type = builder.DefineType(typeNode1);

        fldType.TypeInfo = type;
        type.AddField(fld);
        
        var deps = TypeDependencyHelper.GetFieldDeps(type, type);
        
        deps.ShouldBeEmpty();
    }

    [Fact]
    public void GetFieldDeps_FieldHasSameModuleGenericType_ReturnsCorrect()
    {
        var foreignBuilder = new SymTableBuilder() { ModuleName = "test1" };
        var foreignTypeNode = new TypedefNode(new TypedefNameNode("Foreign"), [], []);
        var foreignType = foreignBuilder.DefineType(foreignTypeNode);


        var builder = new SymTableBuilder() { ModuleName = "test2" };

        var generic = new GenericDefinitionNode(new GenericParameterNameNode("T"));
        var typeNode1 = new TypedefNode(new TypedefNameNode("A"), [], [generic]);

        var type1 = builder.DefineType(typeNode1, [generic]);
        var type1Impl = type1.MakeGenericType([foreignType]);

        var fldType = new TypeNode(new TypeNameNode("A[Foreign]")){ TypeInfo = type1Impl };
        var fld = new FieldDefNode(fldType, new FieldNameNode("ff"));
        var typeNode2 = new TypedefNode(new TypedefNameNode("B"), [fld], []);
        
        var type2 = builder.DefineType(typeNode2);
        type2.AddField(fld);

        type1Impl.ShouldNotBeNull();
        var deps = TypeDependencyHelper.GetFieldDeps(type1Impl, type2);
        var dep = deps.ShouldHaveSingleItem();
        dep.ShouldBe(type1);
    }

    [Fact]
    public void GetFieldDeps_FieldHasGenericParameterDeps_Correct()
    {
        var foreignBuilder = new SymTableBuilder() { ModuleName = "test1" };
        var foreignGeneric = new GenericDefinitionNode(new GenericParameterNameNode("T"));
        var foreignTypeNode = new TypedefNode(new TypedefNameNode("Foreign"), [], [foreignGeneric]);
        var foreignType = foreignBuilder.DefineType(foreignTypeNode, [foreignGeneric]);
        
        var builder = new SymTableBuilder(){ModuleName = "test2"};
        var fieldArgTypeNode = new TypedefNode(new TypedefNameNode("A"), [], []);
        var fieldArgType = builder.DefineType(fieldArgTypeNode);

        var fieldTypeImpl = foreignType.MakeGenericType([fieldArgType]);

        var fieldTypeNode = new TypeNode(new TypeNameNode("Foreign[A]")) { TypeInfo = fieldTypeImpl };
        var fieldNode = new FieldDefNode(fieldTypeNode, new FieldNameNode("ff"));

        var typeNode = new TypedefNode(new TypedefNameNode("B"), [fieldNode], []);
        var type = builder.DefineType(typeNode);
        type.AddField(fieldNode);
        
        fieldTypeImpl.ShouldNotBeNull();
        var deps = TypeDependencyHelper.GetFieldDeps(fieldTypeImpl, type);
        
        var dep = deps.ShouldHaveSingleItem();
        dep.ShouldBe(fieldArgType);
    }

    [Fact]
    public void GetFieldDepsFromFullyModuleGeneric_Correct()
    {
        var builder = new SymTableBuilder();

        var genericParam = new GenericDefinitionNode(new GenericParameterNameNode("T"));
        var genericDefNode = new TypedefNode(new TypedefNameNode("A"), [], [genericParam]);

        var genericDef = builder.DefineType(genericDefNode, [genericParam]);

        var argDefNode = new TypedefNode(new TypedefNameNode("B"), [], []);
        var argDef = builder.DefineType(argDefNode);
        
        
        var fldType = genericDef.MakeGenericType([argDef]);
        var fldTypeNode = new  TypeNode(new TypeNameNode("A[B]")) { TypeInfo = fldType };
        
        var fld = new FieldDefNode(fldTypeNode, new FieldNameNode("ff"));
        var definingTypeNode = new TypedefNode(new  TypedefNameNode("C"), [fld], []);
        
        var definingType = builder.DefineType(definingTypeNode);

        fldType.ShouldNotBeNull();
        var deps = TypeDependencyHelper.GetFieldDeps(fldType, definingType);
        
        deps.Count.ShouldBe(2);
        deps.ShouldContain(genericDef);
        deps.ShouldContain(argDef);
    }
}