using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

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
        const string methodName = "Test";
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, to, [from]);

        var inParam = new TestParameter(from, "toCast");

        const string castResName = "castRes";
        var methodBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(to), new MemberNode(castResName)),
            new AssignNode(new MemberNode(castResName), EmissionSetupHelper.CreateCastNode(from, to, new MemberNode(inParam.Name))),
            new ReturnNode(new MemberNode(castResName))
        ]);

        var context = new CompilerEmissionContext(methodBody, methodBuilder, [inParam], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        
        var type = typeBuilder.CreateType();
        var (typeInstance, methodInfo) = EmissionSetupHelper.CreateObject(type, methodName);
        instance ??= Activator.CreateInstance(from);
        var res = methodInfo!.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to, res.GetType());
    }

    //I cant create short type variable through obj
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
        const string methodName = "Test";
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, to, [from]);

        var inParam = new TestParameter(from, "toCast");

        const string castResName = "castRes";
        /*
         * T castRes
         * castRes = (T)arg
         * return castRes
         */
        var methodBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(to), new MemberNode(castResName)),
            new AssignNode(new MemberNode(castResName), EmissionSetupHelper.CreateCastNode(from, to, new MemberNode(inParam.Name))),
            new ReturnNode(new MemberNode(castResName))
        ]);

        var context = new CompilerEmissionContext(methodBody, methodBuilder, [inParam], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        
        var type = typeBuilder.CreateType();
        var (typeInstance, methodInfo) = EmissionSetupHelper.CreateObject(type, methodName);
        return (methodInfo, typeInstance)!;
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

        //In runtime no difference between child instance and parent type
        // yield return [typeof(int), typeof(object), 233];
        // yield return [typeof(ExampleChild), typeof(ExampleParent), new ExampleChild()];
        // yield return [typeof(ExampleChild), typeof(object), new ExampleChild()];
        // yield return [typeof(ExampleChild), typeof(IExampleInterface), new ExampleChild()];
        // yield return [typeof(ExampleParent), typeof(object), new ExampleParent()];
        // yield return [typeof(ExampleStruct), typeof(IExampleInterface), new ExampleStruct()];
        // yield return [typeof(string), typeof(object), "abc"];
    }
}