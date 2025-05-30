using System.Reflection;
using System.Reflection.Emit;
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

/// <summary>
/// Here lives maths
/// </summary>
public class MathEmissionTests
{
    [Theory]
    [MemberData(nameof(IncrementDecrementDataProvider))]
    public async Task EmitIncrementDecrement(NodeBase ast, 
        object resultShould, Type resultTypeShould,
        object assignShould, Type assignTypeShould)
    {
        //Check incremental
        var retAstA = new BodyNode(
        [
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new MemberNode("a")),
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new MemberNode("b")),
            new AssignNode(new MemberNode("b"), ast),
            new ReturnNode(new MemberNode("a"))
        ]);
        await EmitCore(retAstA, resultShould, resultTypeShould);
        
        //Check increment assign
        var retAstB = new BodyNode(
        [
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new MemberNode("a")),
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new MemberNode("b")),
            new AssignNode(new MemberNode("b"), ast),
            new ReturnNode(new MemberNode("b"))
        ]);
        await EmitCore(retAstB, assignShould, assignTypeShould);
    }

    /*
     Emitter cannot emit return with expression we need put every simple expression in variable first
     Opcode count optimization will be later
     */
    [Theory]
    [MemberData(nameof(SimpleMathDataProvider))]
    public async Task EmitSimpleMath(NodeBase ast, object resultShould, Type resultTypeShould)
    {
        var retAst = new BodyNode(
        [
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new MemberNode("a")),
            new AssignNode(new MemberNode("a"), ast),
            new ReturnNode(new MemberNode("a"))
        ]);
        await EmitCore(retAst, resultShould, resultTypeShould);
    }


    private async Task EmitCore(BodyNode ast, object resultShould, Type resultTypeShould)
    {
        const string methodName = "Test";
        var (_, typeBuilder, methodBuilder, _)
            = EmissionSetupHelper.CreateMethodBuilder(methodName, resultTypeShould, []);

        var context = new CompilerEmissionContext(ast, methodBuilder, [], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();

        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance, [])!;
        var resType = res.GetType();
        Assert.Equal(resultTypeShould, resType);
        Assert.Equal(resultShould, res);

    }

    public static IEnumerable<object[]> SimpleMathDataProvider()
    {
        var firstLiteral = new LiteralNode(2, typeof(int));
        var secondLiteral = new LiteralNode(-3, typeof(int));
        var trueLiteral = new LiteralNode(true, typeof(bool));
        var falseLiteral = new LiteralNode(false, typeof(bool));
        float fl1 = 3.14f, fl2 = 6.81f;
        var firstFloat = new LiteralNode(fl1, typeof(float));
        var secondFloat = new LiteralNode(fl2, typeof(float));
        double d1 = 3e-8d, d2 = 6.81d;
        var firstDouble = new LiteralNode(d1, typeof(double));
        var secondDouble = new LiteralNode(d2, typeof(double));
        yield return [new PlusNode(firstLiteral, secondLiteral), -1, typeof(int)];
        yield return [new MinusNode(firstLiteral, secondLiteral), 5, typeof(int)];
        yield return [new MultiplyNode(firstLiteral, secondLiteral), -6, typeof(int)];
        yield return [new DivideNode(firstLiteral, secondLiteral), 0, typeof(int)];
        yield return [new PlusNode(firstFloat, secondFloat), fl1 + fl2, typeof(float)];
        yield return [new MinusNode(firstFloat, secondFloat), fl1 - fl2, typeof(float)];
        yield return [new MultiplyNode(firstFloat, secondFloat), fl1 * fl2, typeof(float)];
        yield return [new DivideNode(firstFloat, secondFloat), fl1 / fl2, typeof(float)];
        yield return [new PlusNode(firstDouble, secondDouble), d1 + d2, typeof(double)];
        yield return [new MinusNode(firstDouble, secondDouble), d1 - d2, typeof(double)];
        yield return [new MultiplyNode(firstDouble, secondDouble), d1 * d2, typeof(double)];
        yield return [new DivideNode(firstDouble, secondDouble), d1 / d2, typeof(double)];
        yield return [new EqualNode(firstLiteral, secondLiteral), false, typeof(bool)];
        yield return [new EqualNode(firstLiteral, firstLiteral), true, typeof(bool)];
        yield return [new NotEqualNode(firstLiteral, secondLiteral), true, typeof(bool)];
        yield return [new NotEqualNode(secondLiteral, secondLiteral), false, typeof(bool)];
        yield return [new LessNode(firstLiteral, secondLiteral), false, typeof(bool)];
        yield return [new LessNode(secondLiteral, firstLiteral), true, typeof(bool)];
        yield return [new LessNode(secondLiteral, secondLiteral), false, typeof(bool)];
        yield return [new GreaterNode(firstLiteral, secondLiteral), true, typeof(bool)];
        yield return [new GreaterNode(secondLiteral, firstLiteral), false, typeof(bool)];
        yield return [new GreaterNode(secondLiteral, secondLiteral), false, typeof(bool)];
        yield return [new GreaterOrEqualNode(firstLiteral, secondLiteral), true, typeof(bool)];
        yield return [new GreaterOrEqualNode(firstLiteral, firstLiteral), true, typeof(bool)];
        yield return [new GreaterOrEqualNode(secondLiteral, firstLiteral), false, typeof(bool)];
        yield return [new LessOrEqualNode(firstLiteral, secondLiteral), false, typeof(bool)];
        yield return [new LessOrEqualNode(secondLiteral, firstLiteral), true, typeof(bool)];
        yield return [new LessOrEqualNode(firstLiteral, firstLiteral), true, typeof(bool)];
        yield return [new UnaryMinusNode(firstLiteral), -2, typeof(int)];
        yield return [new UnaryMinusNode(secondLiteral), 3, typeof(int)];
        yield return [new NotNode(trueLiteral), false, typeof(bool)];
        yield return [new NotNode(falseLiteral), true, typeof(bool)];
        yield return [new AndNode(falseLiteral, falseLiteral), false, typeof(bool)];
        yield return [new AndNode(trueLiteral, falseLiteral), false, typeof(bool)];
        yield return [new AndNode(falseLiteral, trueLiteral), false, typeof(bool)];
        yield return [new AndNode(trueLiteral, trueLiteral), true, typeof(bool)];
        yield return [new OrNode(falseLiteral, falseLiteral), false, typeof(bool)];
        yield return [new OrNode(trueLiteral, falseLiteral), true, typeof(bool)];
        yield return [new OrNode(falseLiteral, trueLiteral), true, typeof(bool)];
        yield return [new OrNode(trueLiteral, trueLiteral), true, typeof(bool)];
        yield return [new XorNode(falseLiteral, falseLiteral), false, typeof(bool)];
        yield return [new XorNode(trueLiteral, falseLiteral), true, typeof(bool)];
        yield return [new XorNode(falseLiteral, trueLiteral), true, typeof(bool)];
        yield return [new XorNode(trueLiteral, trueLiteral), false, typeof(bool)];
        yield return [new BitwiseOrNode(firstLiteral, secondLiteral), -1, typeof(int)];
        yield return [new BitwiseAndNode(firstLiteral, secondLiteral), 0, typeof(int)];
        yield return [new XorNode(firstLiteral, secondLiteral), -1, typeof(int)];
    }

    public static IEnumerable<object[]> IncrementDecrementDataProvider()
    {
        var firstLiteral = new MemberNode("a");
        yield return [new PrefixIncrementNode(firstLiteral), 1, typeof(int), 1, typeof(int)];
        yield return [new PrefixDecrementNode(firstLiteral), -1, typeof(int), -1, typeof(int)];
        yield return [new PostfixDecrementNode(firstLiteral), -1, typeof(int), 0 ,typeof(int)];
        yield return [new PostfixIncrementNode(firstLiteral), 1, typeof(int), 0, typeof(int)];
    }
}