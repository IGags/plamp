using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.CodeEmission.Tests.Infrastructure;
using Shouldly;

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
        var instructionList = new List<NodeBase>()
        {
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(resultTypeShould),
                new VariableNameNode("a")),
            new AssignNode(new MemberNode("a"), operatorAst),
            new ReturnNode(new MemberNode("a"))
        };
        instructionList.InsertRange(0, definitions);
        var retAst = new BodyNode(instructionList);
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], retAst, resultTypeShould);
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
        const float fl1 = 3.14f;
        const float fl2 = 6.81f;
        var firstFloat = new LiteralNode(fl1, typeof(float));
        var secondFloat = new LiteralNode(fl2, typeof(float));
        const double d1 = 3e-8d;
        const double d2 = 6.81d;
        var firstDouble = new LiteralNode(d1, typeof(double));
        var secondDouble = new LiteralNode(d2, typeof(double));

        var firstName = new MemberNode("tempConst1");
        var firstVariableName = new VariableNameNode("tempConst1");
        var firstDefInt = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstLiteral.Type), firstVariableName);
        var firstDefDouble = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstDouble.Type), firstVariableName);
        var firstDefFloat = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstFloat.Type), firstVariableName);
        var trueDefBool = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(trueLiteral.Type), firstVariableName);
        
        var secondName = new MemberNode("tempConst2");
        var secondVariableName = new VariableNameNode("tempConst2");
        var secondDefInt = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(secondLiteral.Type), secondVariableName);
        var secondDefDouble = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstDouble.Type), secondVariableName);
        var secondDefFloat = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(firstFloat.Type), secondVariableName);
        var falseDefBool = new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(falseLiteral.Type), secondVariableName);

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
        
        yield return [intDefs, new AddNode(firstName, secondName), -1, typeof(int)];
        yield return [intDefs, new SubNode(firstName, secondName), 5, typeof(int)];
        yield return [intDefs, new MulNode(firstName, secondName), -6, typeof(int)];
        yield return [intDefs, new DivNode(firstName, secondName), 0, typeof(int)];
        yield return [floatDefs, new AddNode(firstName, secondName), fl1 + fl2, typeof(float)];
        yield return [floatDefs, new SubNode(firstName, secondName), fl1 - fl2, typeof(float)];
        yield return [floatDefs, new MulNode(firstName, secondName), fl1 * fl2, typeof(float)];
        yield return [floatDefs, new DivNode(firstName, secondName), fl1 / fl2, typeof(float)];
        yield return [doubleDefs, new AddNode(firstName, secondName), d1 + d2, typeof(double)];
        yield return [doubleDefs, new SubNode(firstName, secondName), d1 - d2, typeof(double)];
        yield return [doubleDefs, new MulNode(firstName, secondName), d1 * d2, typeof(double)];
        yield return [doubleDefs, new DivNode(firstName, secondName), d1 / d2, typeof(double)];
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
    
    
    public class CallbackClass
    {
        public object? InnerVar { get; private set; }
        
        public object? Result { get; private set; }

        public void Callback(object innerVar, object result)
        {
            InnerVar = innerVar;
            Result = result;
        }
    }
    
    [Theory]
    [MemberData(nameof(EmitIncrementDecrementDataProvider))]
    public async Task EmitIncrementDecrement_Success(object inner, TypeNode variableType, Func<NodeBase, NodeBase> createFromInner, object innerShould, object resultShould)
    {
        var objParam = new TestParameter(typeof(CallbackClass), "obj");
        var toType = new TypeNode(new TypeNameNode(nameof(Object)));
        toType.SetType(typeof(object));

        var firstArg = new CastNode(toType, new MemberNode("a"));
        firstArg.SetFromType(variableType.Symbol!);
        
        var secondArg = new CastNode(toType, new MemberNode("b"));
        secondArg.SetFromType(variableType.Symbol!);
        
        var call = new CallNode(new MemberNode("obj"), new FuncCallNameNode(nameof(CallbackClass.Callback)), [firstArg, secondArg]);
        call.SetInfo(typeof(CallbackClass).GetMethod(nameof(CallbackClass.Callback))!);
        
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(variableType, new VariableNameNode("a")), new LiteralNode(inner, variableType.Symbol!)),
            new AssignNode(new VariableDefinitionNode(variableType, new VariableNameNode("b")), createFromInner(new MemberNode("a"))),
            call,
            new ReturnNode(null)
        ]);
        
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([objParam], body, typeof(void));
        var callbackClass = new CallbackClass();
        method!.Invoke(instance, [callbackClass]);
        callbackClass.InnerVar.ShouldBe(innerShould);
        callbackClass.Result.ShouldBe(resultShould);
    }

    public static IEnumerable<object[]> EmitIncrementDecrementDataProvider()
    {
        var intTypeNode = new TypeNode(new TypeNameNode(nameof(Int32)));
        intTypeNode.SetType(typeof(int));
        
        var floatTypeNode = new TypeNode(new TypeNameNode(nameof(Single)));
        floatTypeNode.SetType(typeof(float));
        
        var uintTypeNode = new TypeNode(new TypeNameNode(nameof(UInt32)));
        uintTypeNode.SetType(typeof(uint));
        
        var doubleTypeNode = new TypeNode(new TypeNameNode(nameof(Double)));
        doubleTypeNode.SetType(typeof(double));
        
        var longTypeNode = new TypeNode(new TypeNameNode(nameof(Int64)));
        longTypeNode.SetType(typeof(long));
        
        var ulongTypeNode = new TypeNode(new TypeNameNode(nameof(UInt64)));
        ulongTypeNode.SetType(typeof(ulong));
        
        yield return [1, intTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixIncrementNode(x)), 2, 2];
        yield return [1, intTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixDecrementNode(x)), 0, 0];
        yield return [1, intTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixIncrementNode(x)), 2, 1];
        yield return [1, intTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixDecrementNode(x)), 0, 1];
        
        yield return [1f, floatTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixIncrementNode(x)), 2f, 2f];
        yield return [1f, floatTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixDecrementNode(x)), 0f, 0f];
        yield return [1f, floatTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixIncrementNode(x)), 2f, 1f];
        yield return [1f, floatTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixDecrementNode(x)), 0f, 1f];
        
        yield return [1u, uintTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixIncrementNode(x)), 2u, 2u];
        yield return [1u, uintTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixDecrementNode(x)), 0u, 0u];
        yield return [1u, uintTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixIncrementNode(x)), 2u, 1u];
        yield return [1u, uintTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixDecrementNode(x)), 0u, 1u];
        
        yield return [1d, doubleTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixIncrementNode(x)), 2d, 2d];
        yield return [1d, doubleTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixDecrementNode(x)), 0d, 0d];
        yield return [1d, doubleTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixIncrementNode(x)), 2d, 1d];
        yield return [1d, doubleTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixDecrementNode(x)), 0d, 1d];
        
        yield return [1L, longTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixIncrementNode(x)), 2L, 2L];
        yield return [1L, longTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixDecrementNode(x)), 0L, 0L];
        yield return [1L, longTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixIncrementNode(x)), 2L, 1L];
        yield return [1L, longTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixDecrementNode(x)), 0L, 1L];
        
        yield return [1UL, ulongTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixIncrementNode(x)), 2UL, 2UL];
        yield return [1UL, ulongTypeNode, (Func<NodeBase, NodeBase>)(x => new PrefixDecrementNode(x)), 0UL, 0UL];
        yield return [1UL, ulongTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixIncrementNode(x)), 2UL, 1UL];
        yield return [1UL, ulongTypeNode, (Func<NodeBase, NodeBase>)(x => new PostfixDecrementNode(x)), 0UL, 1UL];
    }
    
    [Theory]
    [MemberData(nameof(MaxValueOverflowDataProvider))]
    public async Task MaxValueOverflow_Success(object value, object one, Type actualType, object should)
    {
        var variableType = new TypeNode(new TypeNameNode(actualType.Name));
        variableType.SetType(actualType);
        var toType = new TypeNode(new TypeNameNode("object"));
        toType.SetType(typeof(object));
        var castToObj = new CastNode(toType, new MemberNode("a"));
        castToObj.SetFromType(actualType);
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(variableType, new VariableNameNode("a")), new LiteralNode(value, actualType)),
            new AssignNode(new MemberNode("a"), new AddNode(new MemberNode("a"), new LiteralNode(one, actualType))),
            new ReturnNode(castToObj)
        ]);
        
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(object));
        var res = method!.Invoke(instance, []);
        res.ShouldBe(should);
    }

    public static IEnumerable<object[]> MaxValueOverflowDataProvider()
    {
        yield return [int.MaxValue, 1, typeof(int), int.MinValue];
        yield return [uint.MaxValue, 1u, typeof(uint), uint.MinValue];
        yield return [short.MaxValue, (short)1, typeof(short), short.MinValue];
        yield return [ushort.MaxValue, (ushort)1, typeof(ushort), ushort.MinValue];
        yield return [byte.MaxValue, (byte)1, typeof(byte), byte.MinValue];
        yield return [long.MaxValue, 1L, typeof(long), long.MinValue];
        yield return [ulong.MaxValue, 1UL, typeof(ulong), ulong.MinValue];
        yield return [float.MaxValue, 1f, typeof(float), float.MaxValue + 1];
        yield return [double.MaxValue, 1d, typeof(double), double.MaxValue + 1];
    }

    [Theory]
    [MemberData(nameof(MaxValueUnderflowDataProvider))]
    public async Task MinValueUnderflow_Success(object value, object one, Type actualType, object should)
    {
        var variableType = new TypeNode(new TypeNameNode(actualType.Name));
        variableType.SetType(actualType);
        var toType = new TypeNode(new TypeNameNode("object"));
        toType.SetType(typeof(object));
        var castToObj = new CastNode(toType, new MemberNode("a"));
        castToObj.SetFromType(actualType);
        var body = new BodyNode(
        [
            new AssignNode(new VariableDefinitionNode(variableType, new VariableNameNode("a")), new LiteralNode(value, actualType)),
            new AssignNode(new MemberNode("a"), new SubNode(new MemberNode("a"), new LiteralNode(one, actualType))),
            new ReturnNode(castToObj)
        ]);
        
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(object));
        var res = method!.Invoke(instance, []);
        res.ShouldBe(should);
    }
    
    public static IEnumerable<object[]> MaxValueUnderflowDataProvider()
    {
        yield return [int.MinValue, 1, typeof(int), int.MaxValue];
        yield return [uint.MinValue, 1u, typeof(uint), uint.MaxValue];
        yield return [short.MinValue, (short)1, typeof(short), short.MaxValue];
        yield return [ushort.MinValue, (ushort)1, typeof(ushort), ushort.MaxValue];
        yield return [byte.MinValue, (byte)1, typeof(byte), byte.MaxValue];
        yield return [long.MinValue, 1L, typeof(long), long.MaxValue];
        yield return [ulong.MinValue, 1UL, typeof(ulong), ulong.MaxValue];
        yield return [float.MinValue, 1f, typeof(float), float.MinValue - 1];
        yield return [double.MinValue, 1d, typeof(double), double.MinValue - 1];
    }
}