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

    [Fact]
    public async Task InsideContinueDoesNotAffectsOutside()
    {
        const string methodName = "DoubleLoop";
        var (_, typeBuilder, methodBuilder, _) =
            EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(int), []);
        
        /*
         * int i = 0
         * int k = 0
         * while(i < 10)
         *     int j = 0
         *     while(j < 10)
         *         j = j + 1
         *         continue
         *         k = k + 1
         *     i = i + 1
         * return i + k
         */
        bool i, j, k;
        const int iterationCount = 10;
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(i))), new LiteralNode(0, typeof(int))),
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(k))), new LiteralNode(0, typeof(int))),
            new WhileNode(new LessNode(new MemberNode(nameof(i)), new LiteralNode(iterationCount, iterationCount.GetType())),
                new BodyNode(
                [
                    new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(j))), new LiteralNode(0, typeof(int))),
                    new WhileNode(
                        new LessNode(new MemberNode(nameof(j)), new LiteralNode(iterationCount, iterationCount.GetType())),
                        new BodyNode(
                        [
                            new AssignNode(new MemberNode(nameof(j)), new PlusNode(new MemberNode(nameof(j)), new LiteralNode(1, typeof(int)))),
                            new ContinueNode(),
                            new AssignNode(new MemberNode(nameof(k)), new PlusNode(new MemberNode(nameof(k)), new LiteralNode(1, typeof(int))))
                        ])),
                    new AssignNode(new MemberNode(nameof(i)), new PlusNode(new MemberNode(nameof(i)), new LiteralNode(1, typeof(int))))
                ])),
            new ReturnNode(new PlusNode(new MemberNode(nameof(i)), new MemberNode(nameof(k))))
        ]);
        var context = new CompilerEmissionContext(body, methodBuilder, [], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);

        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance!, []);
        Assert.Equal(iterationCount, res);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task BreakOnIteration(int iterationNumber)
    {
        const string methodName = "IterationBreak";
        var argType = typeof(int);
        var (_, typeBuilder, methodBuilder, _) =
            EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(int), [argType]);
        var arg = new TestParameter(argType, "n");
        
        /*
         * var i = 0
         * while(true)
         *     if(i == n)
         *         break
         *     i = i + 1
         * return i
         */
        bool i;
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(i))), new LiteralNode(0, typeof(int))),
            new WhileNode(new LiteralNode(true, typeof(bool)),
                new BodyNode(
                [
                    new ConditionNode(
                        new EqualNode(new MemberNode(nameof(i)), new MemberNode(arg.Name)),
                        new BodyNode(
                        [
                            new BreakNode()
                        ]), null),
                    new AssignNode(new MemberNode(nameof(i)), new PlusNode(new MemberNode(nameof(i)), new LiteralNode(1, typeof(int))))
                ])),
            new ReturnNode(new MemberNode(nameof(i)))
        ]);
        var emissionContext = new CompilerEmissionContext(body, methodBuilder, [arg], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(emissionContext, CancellationToken.None);

        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance!, [iterationNumber]);
        Assert.Equal(iterationNumber, res);
    }

    [Fact]
    public async Task InnerBreakDoesNotBreakOuter()
    {
        const string methodName = "innerBreak";
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(int), []);
        
        /*
         * int i = 0
         * while(i < 10)
         *     int j = 0
         *     while(true)
         *         break
         *         j = j + 1
         *     i = i + 1
         * return i + j
         */
        const int iterationCount = 10;
        bool i, j;
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(i))), new LiteralNode(0, typeof(int))),
            new AssignNode(new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(nameof(j))), new LiteralNode(0, typeof(int))),
            new WhileNode(
                new LessNode(new MemberNode(nameof(i)), new LiteralNode(iterationCount, typeof(int))),
                new BodyNode(
                [
                    new WhileNode(new LiteralNode(true, typeof(bool)),
                        new BodyNode(
                        [
                            new BreakNode(),
                            new AssignNode(new MemberNode(nameof(j)), new PlusNode(new MemberNode(nameof(j)), new LiteralNode(1, typeof(int))))
                        ])),
                    new AssignNode(new MemberNode(nameof(i)), new PlusNode(new MemberNode(nameof(i)), new LiteralNode(1, typeof(int))))
                ])),
            new ReturnNode(new PlusNode(new MemberNode(nameof(i)), new MemberNode(nameof(j))))
        ]);
        var context = new CompilerEmissionContext(body, methodBuilder, [], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);

        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);

        var res = method!.Invoke(instance!, []);
        Assert.Equal(iterationCount, res);
    }
}