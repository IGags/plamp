using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative;
using plamp.CodeEmission.Tests.Infrastructure;
using Shouldly;

namespace plamp.CodeEmission.Tests;

/// <summary>
/// Валидация эмиссии создания типов
/// </summary>
public class TypeInitEmissionTests
{
    public static IEnumerable<object[]> EmitBuiltinPrimitiveInit_DataProvider()
    {
        yield return [Builtins.Int];
        yield return [Builtins.Uint];
        yield return [Builtins.Long];
        yield return [Builtins.Ulong];
        yield return [Builtins.Short];
        yield return [Builtins.Ushort];
        yield return [Builtins.Byte];
        yield return [Builtins.Sbyte];
        yield return [Builtins.Float];
        yield return [Builtins.Double];
        yield return [Builtins.Bool];
        yield return [Builtins.Char];
        yield return [Builtins.Void];
    }
    
    /// <summary>
    /// Создать встроенный примитивный тип
    /// </summary>
    [Theory]
    [MemberData(nameof(EmitBuiltinPrimitiveInit_DataProvider))]
    public void EmitBuiltinPrimitiveInit_ThrowsException(ITypeInfo info)
    {
        var ast = new BodyNode(
        [
            new ReturnNode(new InitTypeNode(new TypeNode(new TypeNameNode("primitive"), []){TypeInfo = info}, []))
        ]);


        Should.Throw<Exception>(() => EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(int)));
    }

    /// <summary>
    /// Создать строку
    /// </summary>
    [Fact]
    public void EmitStringInit_CorrectNull()
    {
        var ast = new BodyNode(
        [
            new ReturnNode(new InitTypeNode(new TypeNode(new TypeNameNode("str"), []){TypeInfo = Builtins.String}, []))
        ]);


        var (instance, method) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(string));
        var res = method.ShouldNotBeNull().Invoke(instance, []);
        res.ShouldBeNull();
    }

    /// <summary>
    /// Создать тип any - возвращает null
    /// </summary>
    [Fact]
    public void EmitAnyType_CorrectReturnsNotNull()
    {
        var ast = new BodyNode(
        [
            new ReturnNode(new InitTypeNode(new TypeNode(new TypeNameNode("any"), []){TypeInfo = Builtins.Any}, []))
        ]);
        
        var (instance, method) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(object));
        var res = method.ShouldNotBeNull().Invoke(instance, []);
        res.ShouldNotBeNull().ShouldBeOfType<object>();
    }

    /// <summary>
    /// Инициплизировать абстрактный тип массива
    /// </summary>
    [Fact]
    public void EmitArrayTypeInit_Throws()
    {
        var ast = new BodyNode(
        [
            new ReturnNode(new InitTypeNode(new TypeNode(new TypeNameNode("array"), []){TypeInfo = Builtins.Array}, []))
        ]);
        
        Should.Throw<Exception>(() => EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(Array)));
    }

    /// <summary>
    /// Инициализировать конкретный тип массива
    /// </summary>
    [Fact]
    public void EmitCorrectArrayInit_ReturnsNull()
    {
        var ast = new BodyNode(
        [
            new ReturnNode(new InitTypeNode(new TypeNode(new TypeNameNode("[]int"), []){TypeInfo = Builtins.Int.MakeArrayType()}, []))
        ]);
        
        var (instance, method) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(int[]));
        var res = method.ShouldNotBeNull().Invoke(instance, []);
        res.ShouldBeNull();
    }

    /// <summary>
    /// Инициализировать структуру
    /// </summary>
    [Fact]
    public void EmitStructureInit_Correct()
    {
        var ast = new BodyNode(
        [
            new ReturnNode(new InitTypeNode(new TypeNode(new TypeNameNode("date"), []){TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(DateTime))}, []))
        ]);
        
        var (instance, method) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(DateTime));
        var res = method.ShouldNotBeNull().Invoke(instance, []);
        res.ShouldBe(new DateTime());
    }
}