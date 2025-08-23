using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.CodeEmission.Tests.Infrastructure;

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
        var arg = new TestParameter(argType, "a");
        var body = new BodyNode(
        [
            new ReturnNode(new MemberNode("a"))
        ]);

        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, argType);
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
        var argType = typeof(int);
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
                new VariableNameNode(tempVarName)
                ),
            new AssignNode(
                new MemberNode(tempVarName), 
                new AddNode(
                    new MemberNode(p1.Name), 
                    new MemberNode(p2.Name)
                    )
                ),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var returnType = typeof(int);
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([p1, p2], body, returnType);
        var res = method!.Invoke(instance, [first, second])!;
        Assert.Equal(998, res);
        Assert.Equal(returnType, res.GetType());
    }
}