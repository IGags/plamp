using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.CodeEmission.Tests;

public class LoopEmissionTests
{
    [Fact]
    public async Task EmitWhileLoop()
    {
        const string methodName = "WhileIter";
        var argType = typeof(int);
        var (_, typeBuilder, methodBuilder, _) =
            EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(int), [argType]);
        var arg = new TestParameter(argType, "n");

        /*
         * int iter = 0
         * while(iter < n)
         *     iter = iter + 1
         * return iter
         */
        bool iter;
        
        var body = new BodyNode(
        [
            new AssignNode(
                new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(iter))),
                new LiteralNode(0, typeof(int))),
            new WhileNode(
                new LessNode(new MemberNode(nameof(iter)), new MemberNode(arg.Name)),
                new BodyNode(
                [
                    new AssignNode(
                        new MemberNode(nameof(iter)),
                        new PlusNode(
                            new MemberNode(nameof(iter)),
                            new LiteralNode(1, typeof(int))))
                ])),
            new ReturnNode(new MemberNode(nameof(iter)))
        ]);

        var context = new CompilerEmissionContext(body, methodBuilder, [arg], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);

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
        const string methodName = "EternalWhileIter";
        var argType = typeof(CancellationToken);
        var (_, typeBuilder, methodBuilder, _) =
            EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(void), [argType]);
        var arg = new TestParameter(argType, "cancellation");
        
        /*
         * while(!cancellation.IsCancellationRequested)
         *     nop
         * return
         */

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
        
        var context = new CompilerEmissionContext(body, methodBuilder, [arg], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        method!.Invoke(instance, [cts.Token]);
    }
}