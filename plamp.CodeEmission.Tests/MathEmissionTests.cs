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
    /*
     Emitter cannot emit return with expression we need put every simple expression in variable first
     Opcode count optimization will be later
     */
    [Theory]
    [MemberData(nameof(SimpleMathDataProvider))]
    public async Task EmitSimpleMath(NodeBase[] definitions, NodeBase operatorAst, object resultShould, Type resultTypeShould)
    {
        //Почти паскаль - переменные в начале :^)
        //Пока не оптимизируем il. Модули оптимизации - отдельная тема. Напишем их позже
        
        /*
         * var tmpConst1
         * var tmpConst2
         * tmpConst1 = val1
         * tmpConst2 = val2
         * var a
         * a = tmpConst1 + tmpConst2
         * return a
         */
        var retAst = new BodyNode(
        [
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new MemberNode("a")),
            new AssignNode(new MemberNode("a"), operatorAst),
            new ReturnNode(new MemberNode("a"))
        ]);
        retAst.InstructionList.InsertRange(0, definitions);
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

        var firstName = new MemberNode("tempConst1");
        var firstDefInt = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstLiteral.Type), firstName);
        var firstDefDouble = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstDouble.Type), firstName);
        var firstDefFloat = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstFloat.Type), firstName);
        var trueDefBool = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(trueLiteral.Type), firstName);
        
        var secondName = new MemberNode("tempConst2");
        var secondDefInt = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(secondLiteral.Type), secondName);
        var secondDefDouble = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstDouble.Type), secondName);
        var secondDefFloat = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstFloat.Type), secondName);
        var falseDefBool = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(falseLiteral.Type), secondName);

        var intDefs = new NodeBase[]
        {
            firstDefInt,
            secondDefInt,
            new AssignNode(firstName, firstLiteral),
            new AssignNode(secondName, secondLiteral)
        };
        
        var doubleDefs = new NodeBase[]
        {
            firstDefDouble,
            secondDefDouble,
            new AssignNode(firstName, firstDouble),
            new AssignNode(secondName, secondDouble)
        };
        
        var floatDefs = new NodeBase[]
        {
            firstDefFloat,
            secondDefFloat,
            new AssignNode(firstName, firstFloat),
            new AssignNode(secondName, secondFloat)
        };
        
        var trueName = new MemberNode("tempConst1");
        var falseName = new MemberNode("tempConst2");
        var boolDefs = new NodeBase[]
        {
            trueDefBool,
            falseDefBool,
            new AssignNode(trueName, trueLiteral),
            new AssignNode(falseName, falseLiteral)
        };
        
        yield return [intDefs, new PlusNode(firstName, secondName), -1, typeof(int)];
        yield return [intDefs, new MinusNode(firstName, secondName), 5, typeof(int)];
        yield return [intDefs, new MultiplyNode(firstName, secondName), -6, typeof(int)];
        yield return [intDefs, new DivideNode(firstName, secondName), 0, typeof(int)];
        yield return [floatDefs, new PlusNode(firstName, secondName), fl1 + fl2, typeof(float)];
        yield return [floatDefs, new MinusNode(firstName, secondName), fl1 - fl2, typeof(float)];
        yield return [floatDefs, new MultiplyNode(firstName, secondName), fl1 * fl2, typeof(float)];
        yield return [floatDefs, new DivideNode(firstName, secondName), fl1 / fl2, typeof(float)];
        yield return [doubleDefs, new PlusNode(firstName, secondName), d1 + d2, typeof(double)];
        yield return [doubleDefs, new MinusNode(firstName, secondName), d1 - d2, typeof(double)];
        yield return [doubleDefs, new MultiplyNode(firstName, secondName), d1 * d2, typeof(double)];
        yield return [doubleDefs, new DivideNode(firstName, secondName), d1 / d2, typeof(double)];
        yield return [intDefs, new EqualNode(firstName, secondName), false, typeof(bool)];
        yield return [intDefs, new EqualNode(firstName, firstName), true, typeof(bool)];
        yield return [intDefs, new NotEqualNode(firstName, secondName), true, typeof(bool)];
        yield return [intDefs, new NotEqualNode(secondName, secondName), false, typeof(bool)];
        yield return [intDefs, new LessNode(firstName, secondName), false, typeof(bool)];
        yield return [intDefs, new LessNode(secondName, firstName), true, typeof(bool)];
        yield return [intDefs, new LessNode(secondName, secondName), false, typeof(bool)];
        yield return [intDefs, new GreaterNode(firstName, secondName), true, typeof(bool)];
        yield return [intDefs, new GreaterNode(secondName, firstName), false, typeof(bool)];
        yield return [intDefs, new GreaterNode(secondName, secondName), false, typeof(bool)];
        yield return [intDefs, new GreaterOrEqualNode(firstName, secondName), true, typeof(bool)];
        yield return [intDefs, new GreaterOrEqualNode(firstName, firstName), true, typeof(bool)];
        yield return [intDefs, new GreaterOrEqualNode(secondName, firstName), false, typeof(bool)];
        yield return [intDefs, new LessOrEqualNode(firstName, secondName), false, typeof(bool)];
        yield return [intDefs, new LessOrEqualNode(secondName, firstName), true, typeof(bool)];
        yield return [intDefs, new LessOrEqualNode(firstName, firstName), true, typeof(bool)];
        yield return [intDefs, new UnaryMinusNode(firstName), -2, typeof(int)];
        yield return [intDefs, new UnaryMinusNode(secondName), 3, typeof(int)];
        yield return [boolDefs, new NotNode(trueName), false, typeof(bool)];
        yield return [boolDefs, new NotNode(falseName), true, typeof(bool)];
        yield return [boolDefs, new AndNode(trueName, falseName), false, typeof(bool)];
        yield return [boolDefs, new AndNode(trueName, falseName), false, typeof(bool)];
        yield return [boolDefs, new AndNode(falseName, trueName), false, typeof(bool)];
        yield return [boolDefs, new AndNode(trueName, trueName), true, typeof(bool)];
        yield return [boolDefs, new OrNode(falseName, falseName), false, typeof(bool)];
        yield return [boolDefs, new OrNode(trueName, falseName), true, typeof(bool)];
        yield return [boolDefs, new OrNode(falseName, trueName), true, typeof(bool)];
        yield return [boolDefs, new OrNode(trueName, trueName), true, typeof(bool)];
        yield return [boolDefs, new XorNode(falseName, falseName), false, typeof(bool)];
        yield return [boolDefs, new XorNode(trueName, falseName), true, typeof(bool)];
        yield return [boolDefs, new XorNode(falseName, trueName), true, typeof(bool)];
        yield return [boolDefs, new XorNode(trueName, trueName), false, typeof(bool)];
        yield return [intDefs, new BitwiseOrNode(firstName, secondName), -1, typeof(int)];
        yield return [intDefs, new BitwiseAndNode(firstName, secondName), 0, typeof(int)];
        yield return [intDefs, new XorNode(firstName, secondName), -1, typeof(int)];
    }
}