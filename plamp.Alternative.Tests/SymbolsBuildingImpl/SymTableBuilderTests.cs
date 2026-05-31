using System;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class SymTableBuilderTests
{
    /// <summary>
    /// Имя модуля должно сохраняться в билдере и использоваться при создании членов модуля.
    /// </summary>
    [Fact]
    public void ModuleName_SetAndRead_Correct()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };

        builder.ModuleName.ShouldBe("testModule");
    }

    /// <summary>
    /// DefineType объявляет тип, добавляет его в список типов и делает доступным через поиск по имени.
    /// </summary>
    [Fact]
    public void DefineType_StoresTypeAndMakesItSearchable()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var typeNode = new TypedefNode(new TypedefNameNode("Person"), [], []);

        var typeInfo = builder.DefineType(typeNode);

        typeInfo.Name.ShouldBe("Person");
        typeInfo.DefinitionName.ShouldBe("Person");
        typeInfo.ModuleName.ShouldBe("testModule");
        builder.ListTypes().ShouldHaveSingleItem().ShouldBe(typeInfo);
        builder.FindType("Person").ShouldBe(typeInfo);
        builder.ContainsSymbol("Person").ShouldBeTrue();
    }

    /// <summary>
    /// Для явно объявленного типа можно получить исходный AST-узел и информацию по имени.
    /// </summary>
    [Fact]
    public void DefineType_StoresDefinitionMapping()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var typeNode = new TypedefNode(new TypedefNameNode("Person"), [], []);

        var typeInfo = builder.DefineType(typeNode);

        builder.TryGetInfo("Person", out ITypeBuilderInfo? foundType).ShouldBeTrue();
        foundType.ShouldBe(typeInfo);
        builder.TryGetDefinition(typeInfo, out var foundNode).ShouldBeTrue();
        foundNode.ShouldBe(typeNode);
    }

    /// <summary>
    /// Пустой список generic-параметров трактуется как обычный, не generic тип.
    /// </summary>
    [Fact]
    public void DefineTypeWithEmptyGenerics_CreatesNonGenericType()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var typeNode = new TypedefNode(new TypedefNameNode("Person"), [], []);

        var typeInfo = builder.DefineType(typeNode, []);

        typeInfo.IsGenericTypeDefinition.ShouldBeFalse();
        typeInfo.GetGenericParameters().ShouldBeEmpty();
    }

    /// <summary>
    /// Непустой список generic-параметров создаёт объявление generic-типа.
    /// </summary>
    [Fact]
    public void DefineTypeWithGenerics_CreatesGenericTypeDefinition()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var generic = new GenericParameterBuilder("T", "testModule");
        var typeNode = new TypedefNode(new TypedefNameNode("Box"), [], [
            new GenericDefinitionNode(new GenericParameterNameNode("T"))
        ]);

        var typeInfo = builder.DefineType(typeNode, [generic]);

        typeInfo.Name.ShouldBe("Box[T]");
        typeInfo.IsGenericTypeDefinition.ShouldBeTrue();
        typeInfo.GetGenericParameters().ShouldHaveSingleItem().ShouldBe(generic);
    }

    /// <summary>
    /// В рамках модуля нельзя повторно объявить тип с уже занятым именем.
    /// </summary>
    [Fact]
    public void DefineTypeWithDuplicateTypeName_Throws()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var typeNode = new TypedefNode(new TypedefNameNode("Person"), [], []);
        builder.DefineType(typeNode);

        Should.Throw<InvalidOperationException>(() => builder.DefineType(typeNode));
    }

    /// <summary>
    /// В рамках модуля нельзя объявить тип с именем уже объявленной функции.
    /// </summary>
    [Fact]
    public void DefineTypeWithExistingFuncName_Throws()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        builder.DefineFunc(new FuncNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FuncNameNode("Person"),
            [],
            [],
            new BodyNode([])));
        var typeNode = new TypedefNode(new TypedefNameNode("Person"), [], []);

        Should.Throw<InvalidOperationException>(() => builder.DefineType(typeNode));
    }

    /// <summary>
    /// DefineFunc объявляет функцию, добавляет её в список функций и делает доступной через поиск по имени.
    /// </summary>
    [Fact]
    public void DefineFunc_StoresFuncAndMakesItSearchable()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var funcNode = new FuncNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FuncNameNode("sum"),
            [],
            [
                new ParameterNode(new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int }, new ParameterNameNode("left")),
                new ParameterNode(new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int }, new ParameterNameNode("right"))
            ],
            new BodyNode([]));

        var funcInfo = builder.DefineFunc(funcNode);

        funcInfo.DefinitionName.ShouldBe("sum");
        funcInfo.Name.ShouldBe("sum(int, int)");
        funcInfo.ModuleName.ShouldBe("testModule");
        funcInfo.ReturnType.ShouldBe(Builtins.Int);
        funcInfo.Arguments.Count.ShouldBe(2);
        funcInfo.Arguments[0].Name.ShouldBe("left");
        funcInfo.Arguments[0].Type.ShouldBe(Builtins.Int);
        funcInfo.Arguments[1].Name.ShouldBe("right");
        funcInfo.Arguments[1].Type.ShouldBe(Builtins.Int);
        builder.ListFuncs().ShouldHaveSingleItem().ShouldBe(funcInfo);
        builder.FindFunc("sum").ShouldBe(funcInfo);
        builder.ContainsSymbol("sum").ShouldBeTrue();
    }

    /// <summary>
    /// Для явно объявленной функции можно получить исходный AST-узел и информацию по имени.
    /// </summary>
    [Fact]
    public void DefineFunc_StoresDefinitionMapping()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var funcNode = new FuncNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FuncNameNode("get"),
            [],
            [],
            new BodyNode([]));

        var funcInfo = builder.DefineFunc(funcNode);

        builder.TryGetInfo("get", out IFnBuilderInfo? foundFunc).ShouldBeTrue();
        foundFunc.ShouldBe(funcInfo);
        builder.TryGetDefinition(funcInfo, out var foundNode).ShouldBeTrue();
        foundNode.ShouldBe(funcNode);
    }

    /// <summary>
    /// Непустой список generic-параметров создаёт объявление generic-функции.
    /// </summary>
    [Fact]
    public void DefineFuncWithGenerics_CreatesGenericFuncDefinition()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var generic = new GenericParameterBuilder("T", "testModule");
        var typeNode = new TypeNode(new TypeNameNode("T")) { TypeInfo = generic };
        var funcNode = new FuncNode(
            typeNode,
            new FuncNameNode("identity"),
            [new GenericDefinitionNode(new GenericParameterNameNode("T"))],
            [new ParameterNode(typeNode, new ParameterNameNode("value"))],
            new BodyNode([]));

        var funcInfo = builder.DefineFunc(funcNode, [generic]);

        funcInfo.Name.ShouldBe("identity[T](T)");
        funcInfo.IsGenericFuncDefinition.ShouldBeTrue();
        funcInfo.GetGenericParameters().ShouldHaveSingleItem().ShouldBe(generic);
        funcInfo.GetGenericParameterBuilders().ShouldHaveSingleItem().ShouldBe(generic);
    }

    /// <summary>
    /// Нельзя объявить функцию, если у AST-узла не заполнен тип возвращаемого значения.
    /// </summary>
    [Fact]
    public void DefineFuncWithoutReturnTypeInfo_Throws()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var funcNode = new FuncNode(
            new TypeNode(new TypeNameNode("int")),
            new FuncNameNode("get"),
            [],
            [],
            new BodyNode([]));

        Should.Throw<InvalidOperationException>(() => builder.DefineFunc(funcNode));
    }

    /// <summary>
    /// Нельзя объявить функцию, если у любого параметра не заполнен тип.
    /// </summary>
    [Fact]
    public void DefineFuncWithArgumentWithoutTypeInfo_Throws()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var funcNode = new FuncNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FuncNameNode("set"),
            [],
            [new ParameterNode(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("value"))],
            new BodyNode([]));

        Should.Throw<InvalidOperationException>(() => builder.DefineFunc(funcNode));
    }

    /// <summary>
    /// В рамках модуля нельзя повторно объявить функцию с уже занятым именем.
    /// </summary>
    [Fact]
    public void DefineFuncWithDuplicateFuncName_Throws()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var funcNode = new FuncNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FuncNameNode("get"),
            [],
            [],
            new BodyNode([]));
        builder.DefineFunc(funcNode);

        Should.Throw<InvalidOperationException>(() => builder.DefineFunc(funcNode));
    }

    /// <summary>
    /// В рамках модуля нельзя объявить функцию с именем уже объявленного типа.
    /// </summary>
    [Fact]
    public void DefineFuncWithExistingTypeName_Throws()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        builder.DefineType(new TypedefNode(new TypedefNameNode("Person"), [], []));
        var funcNode = new FuncNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FuncNameNode("Person"),
            [],
            [],
            new BodyNode([]));

        Should.Throw<InvalidOperationException>(() => builder.DefineFunc(funcNode));
    }

    /// <summary>
    /// CreateGenericParameter создаёт не привязанный к таблице generic-параметр с именем из AST и текущим модулем.
    /// </summary>
    [Fact]
    public void CreateGenericParameter_CreatesDetachedParameter()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var genericNode = new GenericDefinitionNode(new GenericParameterNameNode("T"));

        var generic = builder.CreateGenericParameter(genericNode);

        generic.Name.ShouldBe("T");
        generic.DefinitionName.ShouldBe("T");
        generic.ModuleName.ShouldBe("testModule");
        generic.IsGenericTypeParameter.ShouldBeTrue();
        builder.ContainsSymbol("T").ShouldBeFalse();
        builder.ListTypes().ShouldBeEmpty();
        builder.ListFuncs().ShouldBeEmpty();
    }

    /// <summary>
    /// Поиск отсутствующих символов возвращает null или false, а пустое имя типа не ищется в пользовательской таблице.
    /// </summary>
    [Fact]
    public void MissingSymbols_ReturnNullOrFalse()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };

        builder.FindType("Missing").ShouldBeNull();
        builder.FindType("").ShouldBeNull();
        builder.FindFunc("missing").ShouldBeNull();
        builder.ContainsSymbol("missing").ShouldBeFalse();
        builder.TryGetInfo("Missing", out ITypeBuilderInfo? typeInfo).ShouldBeFalse();
        typeInfo.ShouldBeNull();
        builder.TryGetInfo("missing", out IFnBuilderInfo? fnInfo).ShouldBeFalse();
        fnInfo.ShouldBeNull();
    }

    /// <summary>
    /// Запрос определения для объектов, которых нет в таблице, должен возвращать false и null.
    /// </summary>
    [Fact]
    public void TryGetDefinitionForForeignInfo_ReturnsFalse()
    {
        var builder = new SymTableBuilder { ModuleName = "testModule" };
        var foreignType = new TypeBuilder("Foreign", "testModule");
        var foreignFunc = new BlankFuncInfo("foreign", [], Builtins.Int, "testModule");

        builder.TryGetDefinition(foreignType, out TypedefNode? typeNode).ShouldBeFalse();
        typeNode.ShouldBeNull();
        builder.TryGetDefinition(foreignFunc, out FuncNode? funcNode).ShouldBeFalse();
        funcNode.ShouldBeNull();
    }
}
