using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.CodeEmission.Tests.Infrastructure;
// ReSharper disable EntityNameCapturedOnly.Local

namespace plamp.CodeEmission.Tests;

public class LoopEmissionTests
{
    [Fact]
    public async Task EmitWhileLoop()
    {

        /*
         * int iter = 0
         * while(iter < n)
         *     iter = iter + 1
         * return iter
         */
        bool iter;
        var arg = new TestParameter(typeof(int), "n");
        var body = new BodyNode(
        [
            new AssignNode(
                new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(iter))),
                new LiteralNode(0, typeof(int))),
            new WhileNode(
                new LessNode(new MemberNode(nameof(iter)), new MemberNode(arg.Name)),
                new BodyNode(
                [
                    new AssignNode(
                        new MemberNode(nameof(iter)),
                        new AddNode(
                            new MemberNode(nameof(iter)),
                            new LiteralNode(1, typeof(int))))
                ])),
            new ReturnNode(new MemberNode(nameof(iter)))
        ]);

        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(int));
        for (var i = 0; i < 10; i++)
        {
            var rnd = Random.Shared.Next(10000);
            var res = method!.Invoke(instance, [rnd]);
            Assert.Equal(rnd, res);
        }
        
        var notCalled = method!.Invoke(instance, [-1]);
        Assert.Equal(0, notCalled);
    }

    [Fact]
    public async Task EmitEternalLoop()
    {
        /*
         * while(!cancellation.IsCancellationRequested)
         *     nop
         * return
         */
        var arg = new TestParameter(typeof(CancellationToken), "cancellation");
        var getter = typeof(CancellationToken).GetProperty(
                nameof(CancellationToken.IsCancellationRequested),
                BindingFlags.Instance | BindingFlags.Public)!
            .GetGetMethod()!;
        
        var body = new BodyNode(
        [
            new WhileNode(
                new NotNode(
                    EmissionSetupHelper.CreateCallNode(new MemberNode(arg.Name), getter, [])),
                new BodyNode(
                [
                ])),
            new ReturnNode(null)
        ]);
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(void));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        method!.Invoke(instance, [cts.Token]);
    }
}