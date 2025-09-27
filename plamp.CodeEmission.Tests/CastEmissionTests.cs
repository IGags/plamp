using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.CodeEmission.Tests.Infrastructure;

namespace plamp.CodeEmission.Tests;

public class CastEmissionTests
{
    public class ExampleParent;
    
    
    public class ExampleChild : ExampleParent, IExampleInterface;
    
    public interface IExampleInterface;
    
    public struct ExampleStruct : IExampleInterface;
    
    /// <summary>
    /// Emit cast object to its parent
    /// </summary>
    [Theory]
    [MemberData(nameof(CastEmissionDataProvider))]
    public async Task EmitCastAsync(Type from, Type to, object? instance = null)
    {
        var inParam = new TestParameter(from, "toCast");
        const string castResName = "castRes";
        var methodBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(to), new VariableNameNode(castResName)),
            new AssignNode(new MemberNode(castResName), EmissionSetupHelper.CreateCastNode(from, to, new MemberNode(inParam.Name))),
            new ReturnNode(new MemberNode(castResName))
        ]);

        var (typeInstance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([inParam], methodBody, to);
        instance ??= Activator.CreateInstance(from);
        var res = methodInfo!.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to, res.GetType());
    }

    //I can't store object of type in object typed variable
    [Fact]
    public async Task EmitShortIntConversion()
    {
        var to = typeof(int);
        var from = typeof(short);
        const short instance = 31;
        var (methodInfo, typeInstance) = await CreateConversionDelegate(from, to);
        var res = methodInfo.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to, res.GetType());
    }
    
    [Fact]
    public async Task EmitByteIntConversion()
    {
        var to = typeof(int);
        var from = typeof(byte);
        const byte instance = 31;
        var (methodInfo, typeInstance) = await CreateConversionDelegate(from, to);
        var res = methodInfo.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to, res.GetType());
    }

    private async Task<(MethodInfo, object)> CreateConversionDelegate(Type from, Type to)
    {
        var inParam = new TestParameter(from, "toCast");

        const string castResName = "castRes";
        /*
         * T castRes
         * castRes = (T)arg
         * return castRes
         */
        var methodBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(to), new VariableNameNode(castResName)),
            new AssignNode(new MemberNode(castResName), EmissionSetupHelper.CreateCastNode(from, to, new MemberNode(inParam.Name))),
            new ReturnNode(new MemberNode(castResName))
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([inParam], methodBody, to);
        return (methodInfo, instance)!;
    }

    public static IEnumerable<object[]> CastEmissionDataProvider()
    {
        yield return [typeof(IExampleInterface), typeof(ExampleChild), new ExampleChild()];
        yield return [typeof(IExampleInterface), typeof(ExampleStruct), new ExampleStruct()];
        yield return [typeof(object), typeof(int), 322];
        yield return [typeof(object), typeof(ExampleChild), new ExampleChild()];
        yield return [typeof(object), typeof(ExampleParent), new ExampleParent()];
        yield return [typeof(ExampleParent), typeof(ExampleChild), new ExampleChild()];
        yield return [typeof(long), typeof(int), 13];
        yield return [typeof(int), typeof(long), 91];
        yield return [typeof(object), typeof(string), "cab"];
        yield return [typeof(float), typeof(double), 1.32f];
        yield return [typeof(double), typeof(float), 1.32D];
        yield return [typeof(double), typeof(int), 1.32D];
        yield return [typeof(int), typeof(double), 1];
        yield return [typeof(int), typeof(short), 222];
        yield return [typeof(int), typeof(byte), 112];
        yield return [typeof(int), typeof(int), 22];
        yield return [typeof(long), typeof(ulong), 41412];
        yield return [typeof(int), typeof(char), 64];
        yield return [typeof(char), typeof(int), 's'];

        //In runtime there is no difference between child instance and parent type
        // yield return [typeof(int), typeof(object), 233];
        // yield return [typeof(ExampleChild), typeof(ExampleParent), new ExampleChild()];
        // yield return [typeof(ExampleChild), typeof(object), new ExampleChild()];
        // yield return [typeof(ExampleChild), typeof(IExampleInterface), new ExampleChild()];
        // yield return [typeof(ExampleParent), typeof(object), new ExampleParent()];
        // yield return [typeof(ExampleStruct), typeof(IExampleInterface), new ExampleStruct()];
        // yield return [typeof(string), typeof(object), "abc"];
    }

    [Fact]
    public async Task LiteralCast_Correct()
    {
        /*
         * return (float)43;
         */
        var to = new TypeNode(new TypeNameNode("float"));
        to.SetType(typeof(float));
        const int literal = 43;
        var cast = new CastNode(to, new LiteralNode(literal, typeof(int)));
        cast.SetFromType(typeof(int));
        
        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], methodBody, typeof(float));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(float), res.GetType());
        Assert.Equal((float)literal, res);
    }

    public static int Example() => 14;

    [Fact]
    public async Task MethodCallCast_Correct()
    {
        /*
         * return (double)Example();
         */
        var to = new TypeNode(new TypeNameNode("double"));
        to.SetType(typeof(double));
        var call = new CallNode(null, new FuncCallNameNode("Example"), []);
        call.SetInfo(typeof(CastEmissionTests).GetMethod(nameof(Example), BindingFlags.Static | BindingFlags.Public)!);
        var cast = new CastNode(to, call);
        cast.SetFromType(typeof(int));
        
        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], methodBody, typeof(double));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(double), res.GetType());
        Assert.Equal((double)14, res);
    }

    [Fact]
    public async Task BinaryCast_Correct()
    {
        /*
         * return (short)(32768 - 1);
         */
        var to = new TypeNode(new TypeNameNode("short"));
        to.SetType(typeof(short));
        var cast = new CastNode(to, new SubNode(new LiteralNode(32768, typeof(int)), new LiteralNode(1, typeof(int))));
        cast.SetFromType(typeof(int));
        
        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], methodBody, typeof(short));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(short), res.GetType());
        Assert.Equal((short)(32768 - 1), res);
    }

    [Fact]
    public async Task UnaryCast_Correct()
    {
        /*
         * return (int)-1.5;
         */
        var to = new TypeNode(new TypeNameNode("int"));
        to.SetType(typeof(int));
        var cast = new CastNode(to, new UnaryMinusNode(new LiteralNode(1.5f, typeof(float))));
        cast.SetFromType(typeof(float));
        
        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], methodBody, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(int), res.GetType());
        Assert.Equal((int)-1.5f, res);
    }

    [Fact]
    public async Task CastCast_Correct()
    {
        /*
         * return (int)1.5;
         */
        var innerType = new TypeNode(new TypeNameNode("double"));
        innerType.SetType(typeof(double));
        var innerCast = new CastNode(innerType, new LiteralNode(1.5f, typeof(float)));
        innerCast.SetFromType(typeof(float));
        var to = new TypeNode(new TypeNameNode("int"));
        to.SetType(typeof(int));
        var cast = new CastNode(to, innerCast);
        cast.SetFromType(typeof(double));
        
        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], methodBody, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(int), res.GetType());
        Assert.Equal((int)(double)1.5f, res);
    }

    [Fact]
    public async Task CastArrayGetter_Correct()
    {
        /*
         * a := [5]double;
         * return int(a[1]);
         */
        var variableType = new TypeNode(new TypeNameNode("double[]"));
        variableType.SetType(typeof(double[]));
        var arrayItemType = new TypeNode(new TypeNameNode("double"));
        arrayItemType.SetType(typeof(double));

        var castToType = new TypeNode(new TypeNameNode("int"));
        castToType.SetType(typeof(int));
        
        var elemGetter = new ElemGetterNode(
            new MemberNode("a"),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))));
        elemGetter.SetItemType(typeof(double));
        
        var castNode = new CastNode(castToType, elemGetter);
        castNode.SetFromType(typeof(double));
        
        var body = new BodyNode(
        [
            new AssignNode(
                new VariableDefinitionNode(variableType, new VariableNameNode("a")), 
                new InitArrayNode(arrayItemType, new LiteralNode(5, typeof(int)))
                ),
            new ReturnNode(castNode)
        ]);
        
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(int), res.GetType());
        Assert.Equal(0, res);
    }
}