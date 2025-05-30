using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.CodeEmission.Tests;

/// <summary>
/// Our funks can take args :^_
/// </summary>
public class ArgPassingTests
{
    [Theory]
    [MemberData(nameof(PassAndReturnArgDataProvider))]
    public async Task PassAndReturnArg(object? value)
    {
        var argType = value == null ? typeof(object) : value.GetType();
        const string methodName = "Test";
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, argType, [argType]);
        var body = new BodyNode(
        [
            new ReturnNode(new MemberNode("a"))
        ]);

        var parameters = new ParameterInfo[]
        {
            new TestParameter(argType, "a")
        };

        var context = new CompilerEmissionContext(body, methodBuilder, parameters, null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();

        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance, [value])!;
        Assert.Equal(value, res);
        if(value != null)
        {
            Assert.Equal(argType, res.GetType());
        }
    }

    public static IEnumerable<object?[]> PassAndReturnArgDataProvider()
    {
        yield return ["123"];
        yield return [123];
        yield return [123L];
        yield return [false];
        yield return [null];
        yield return [new {a = 1, b = "222"}];
    }

    /// <summary>
    /// Прочитать аргументы и сложить их
    /// </summary>
    [Fact]
    public async Task PassMultipleArgs()
    {
        const int first = -1, second = 999;
        const string methodName = "Test";
        var returnType = typeof(int);
        var argType =  typeof(int);
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, returnType, [argType, argType]);

        var p1 = new TestParameter(argType, "p1");
        var p2 = new TestParameter(argType, "p2");
        const string tempVarName = "b";
        
        /*
         * int b
         * b = p1 + p2
         * return b
         */
        var body = new BodyNode(
        [
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(argType),
                new MemberNode(tempVarName)
                ),
            new AssignNode(
                new MemberNode(tempVarName), 
                new PlusNode(
                    new MemberNode(p1.Name), 
                    new MemberNode(p2.Name)
                    )
                ),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var parameters = new ParameterInfo[] {p1, p2};
        var ctx = new CompilerEmissionContext(body, methodBuilder, parameters, null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(ctx, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance, [first, second])!;
        Assert.Equal(998, res);
        Assert.Equal(returnType, res.GetType());
    }
}