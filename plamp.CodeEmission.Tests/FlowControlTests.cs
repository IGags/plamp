using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.CodeEmission.Tests;

public class FlowControlTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(-1, 0)]
    [InlineData(2, 1)]
    [InlineData(10, 5)]
    [InlineData(9, 5)]
    public async Task ContinueOnEvenNumber(int argValue, int resShould)
    {
        const string methodName = "ContinueOnMod2";
        var argType = typeof(int);
        var (_, typeBuilder, methodBuilder, _) =
            EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(int), [argType]);
        var arg = new TestParameter(argType, "n");
        
        /*
         * int iter = 0
         * int t = 0
         * while(iter < n)
         *     iter = iter + 1
         *     if(iter % 2 == 0)
         *         continue
         *     t = t + 1
         * return t
         */
        bool iter, t;
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(iter))), new LiteralNode(0, typeof(int))),
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(t))), new LiteralNode(0, typeof(int))),
            new WhileNode(new LessNode(new MemberNode(nameof(iter)), new MemberNode(arg.Name)),
                new BodyNode(
                [
                    new AssignNode(new MemberNode(nameof(iter)), new PlusNode(new MemberNode(nameof(iter)), new LiteralNode(1, typeof(int)))),
                    new ConditionNode(
                        new EqualNode(
                            new ModuloNode(
                                new MemberNode(nameof(iter)),
                                new LiteralNode(2, typeof(int))),
                            new LiteralNode(0, typeof(int))),
                        new BodyNode(
                        [
                            new ContinueNode()
                        ]), null),
                    new AssignNode(new MemberNode(nameof(t)), new PlusNode(new MemberNode(nameof(t)), new LiteralNode(1, typeof(int))))
                ])),
            new ReturnNode(new MemberNode(nameof(t)))
        ]);

        var context = new CompilerEmissionContext(body, methodBuilder, [arg], null, null);
        var emitter = new DefaultIlCodeEmitter();

        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);

        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance, [argValue]);
        Assert.Equal(resShould, res);
    }
}