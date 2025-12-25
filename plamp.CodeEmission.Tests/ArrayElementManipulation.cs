using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative;
using plamp.CodeEmission.Tests.Infrastructure;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class ArrayElementManipulation
{
    public static int GetZero() => 0;
    
    private List<NodeBase> MakeArrayInitAst()
    {
        /*
         * a := [3]int;
         * a[1] := 1;
         * a[2] := 2;
         */
        var arrayItemType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };

        var arrayType = new TypeNode(new TypeNameNode("[]int"))
        {
            TypeInfo = Builtins.Int.MakeArrayType()
        };

        var assign = new AssignNode(
            [new VariableDefinitionNode(arrayType, new VariableNameNode("a"))],
            [new InitArrayNode(arrayItemType, new LiteralNode(3, Builtins.Int))]);

        var literal1 = new LiteralNode(1, Builtins.Int);
        var indexer1 = new IndexerNode(new MemberNode("a"), literal1)
        {
            ItemType = Builtins.Int
        };
        var setter1 = new AssignNode([indexer1], [literal1]);
        
        var literal2 = new LiteralNode(2, Builtins.Int);
        var indexer2 = new IndexerNode(new MemberNode("a"), literal2)
        {
            ItemType = Builtins.Int
        };
        var setter2 = new AssignNode([indexer2], [literal2]);
        
        return 
        [
            assign,
            setter1,
            setter2
        ];
    }
    
    #region Getter
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetArrayElementByIndex_ReturnsCorrect(int index)
    {
        /*
         * return a[ix];
         */
        var elemGetter = new IndexerNode(new MemberNode("a"), new LiteralNode(index, Builtins.Int))
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var result = methodInfo!.Invoke(instance, []);
        result.ShouldBeOfType<int>().ShouldBe(index);
    }

    [Fact]
    public void GetArrayElementByNegativeIndex_ThrowsOutOfRange()
    {
        /*
         * return a[-1];
         */
        var elemGetter = new IndexerNode(new MemberNode("a"), new LiteralNode(-1, Builtins.Int))
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        Should.Throw<TargetInvocationException>(() => methodInfo!.Invoke(instance, [])).InnerException
            .ShouldBeOfType<IndexOutOfRangeException>();
    }

    [Fact]
    public void GetArrayElementByOverflowIndex_ThrowsOutOfRange()
    {
        /*
         * return a[4];
         */
        var elemGetter = new IndexerNode(new MemberNode("a"), new LiteralNode(4, Builtins.Int))
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        Should.Throw<TargetInvocationException>(() => methodInfo!.Invoke(instance, [])).InnerException
            .ShouldBeOfType<IndexOutOfRangeException>();
    }

    [Fact]
    public void GetArrayElementByUnaryOperatorIndexer_ReturnsCorrect()
    {
        /*
         * i := 0;
         * return a[i++];
         */
        var variableType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };

        var definition = new VariableDefinitionNode(variableType, new VariableNameNode("i"));
        var assign = new AssignNode([definition], [new LiteralNode(0, Builtins.Int)]);
        
        var elemGetter = new IndexerNode(new MemberNode("a"), new PostfixIncrementNode(new MemberNode("i")))
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.AddRange([assign, new ReturnNode(elemGetter)]);
        var body = new BodyNode(bodyItems);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var result = methodInfo!.Invoke(instance, []);
        result.ShouldBeOfType<int>().ShouldBe(0);
    }

    [Fact]
    public void GetArrayElementByBinaryOperatorIndexer_ReturnsCorrect()
    {
        /*
         * return a[1 + 1];
         */
        var elemGetter = new IndexerNode(new MemberNode("a"), new AddNode(new LiteralNode(1, Builtins.Int), new LiteralNode(1, Builtins.Int)))
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var result = methodInfo!.Invoke(instance, []);
        result.ShouldBeOfType<int>().ShouldBe(2);
    }

    [Fact]
    public void GetArrayElementByCastIndexer_ReturnsCorrect()
    {
        /*
         * return a[int(1.0)]
         */
        var castTargetType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };

        var cast = new CastNode(castTargetType, new LiteralNode(1.0, Builtins.Double))
        {
            FromType = Builtins.Double
        };

        var elemGetter = new IndexerNode(new MemberNode("a"), cast)
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var result = methodInfo!.Invoke(instance, []);
        result.ShouldBeOfType<int>().ShouldBe(1);
    }
    
    [Fact]
    public void GetArrayElementByCallIndexer_ReturnsCorrect()
    {
        /*
         * return a[getZero()];
         */
        var call = new CallNode(null, new FuncCallNameNode(nameof(GetZero)), []);
        var info = typeof(ArrayElementManipulation).GetMethod(nameof(GetZero))!;
        call.FnInfo = EmissionSetupHelper.MakeFuncRef(info);
        
        var elemGetter = new IndexerNode(new MemberNode("a"), call)
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var result = methodInfo!.Invoke(instance, []);
        result.ShouldBeOfType<int>().ShouldBe(0);
    }

    [Fact]
    public void GetArrayElementByArrayElementGetter_ReturnsCorrect()
    {
        /*
         * return a[a[1]];
         */
        var elemGetterInner = new IndexerNode(new MemberNode("a"), new LiteralNode(1, Builtins.Int))
        {
            ItemType = Builtins.Int
        };

        var elemGetter = new IndexerNode(new MemberNode("a"), elemGetterInner)
        {
            ItemType = Builtins.Int
        };

        var bodyItems = MakeArrayInitAst();
        bodyItems.Add(new ReturnNode(elemGetter));
        var body = new BodyNode(bodyItems);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var result = methodInfo!.Invoke(instance, []);
        result.ShouldBeOfType<int>().ShouldBe(1);
    }

    #endregion

    #region Setter

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SetArrayElementByIndex_ReturnsSuccess(int indexer)
    {
        /*
         * a[ix] := -ix;
         * return a;
         */
        var indexerNode = new IndexerNode(new MemberNode("a"), new MemberNode("ix"))
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexerNode],
            [new UnaryMinusNode(new MemberNode("ix"))]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var indexerParam = new TestParameter(typeof(int), "ix");
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([indexerParam], body, typeof(int[]));

        var result = methodInfo!.Invoke(instance, [indexer]);
        result.ShouldBeOfType<int[]>();
    }

    [Fact]
    public void SetArrayElementByNegativeIndex_ThrowsOutOfRange()
    {
        /*
         * a[-1] := -1;
         */
        var indexerNode = new IndexerNode(new MemberNode("a"), new LiteralNode(-1, Builtins.Int))
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexerNode], 
            [new LiteralNode(-1, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));

        Should.Throw<TargetInvocationException>(() => methodInfo!.Invoke(instance, []))
            .InnerException.ShouldBeOfType<IndexOutOfRangeException>();
    }

    [Fact]
    public void SetArrayElementByOverflowIndex_ThrowsOutOfRange()
    {
        /*
         * a[4] := 0;
         */
        var indexer = new IndexerNode(new MemberNode("a"), new LiteralNode(4, Builtins.Int))
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexer],
            [new LiteralNode(0, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));

        Should.Throw<TargetInvocationException>(() => methodInfo!.Invoke(instance, []))
            .InnerException.ShouldBeOfType<IndexOutOfRangeException>();
    }

    [Fact]
    public void SetArrayElementByUnaryOperatorIndex_ReturnsCorrect()
    {
        /*
         * a[-(-1)] := 42;
         */
        var indexer = new IndexerNode(new MemberNode("a"), new UnaryMinusNode(new LiteralNode(-1, Builtins.Int)))
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexer],
            [new LiteralNode(42, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int[]>()[1].ShouldBe(42);
    }

    [Fact]
    public void SetArrayElementByBinaryOperatorIndex_ReturnsCorrect()
    {
        /*
         * a[100 - 99] := 98;
         */
        var indexer = new IndexerNode(new MemberNode("a"),
            new SubNode(new LiteralNode(100, Builtins.Int), new LiteralNode(99, Builtins.Int)))
        {
            ItemType = Builtins.Int
        };

        var setter = new AssignNode(
            [indexer],
            [new LiteralNode(98, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int[]>()[1].ShouldBe(98);
    }

    [Fact]
    public void SetArrayElementByCastOperatorIndex_ReturnsCorrect()
    {
        /*
         * a[int(2.0)] := 11;
         */
        var toType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var cast = new CastNode(toType, new LiteralNode(2.0, Builtins.Double))
        {
            FromType = Builtins.Double
        };

        var indexer = new IndexerNode(new MemberNode("a"), cast)
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexer],
            [new LiteralNode(11, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int[]>()[2].ShouldBe(11);
    }

    [Fact]
    public void SetArrayElementByFuncCallIndex_ReturnsCorrect()
    {
        /*
         * a[getZero()] := -99;
         */
        var callNode = new CallNode(null, new FuncCallNameNode("getZero"), []);
        var info = typeof(ArrayElementManipulation).GetMethod(nameof(GetZero))!;
        callNode.FnInfo = EmissionSetupHelper.MakeFuncRef(info);

        var indexer = new IndexerNode(new MemberNode("a"), callNode)
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexer],
            [new LiteralNode(-99, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int[]>()[0].ShouldBe(-99);
    }

    [Fact]
    public void SetArrayElementByArrayElementIndexer_ReturnsCorrect()
    {
        /*
         * a[a[1]] := -1;
         */
        var getterNode = new IndexerNode(new MemberNode("a"), new LiteralNode(1, Builtins.Int))
        {
            ItemType = Builtins.Int
        };

        var indexer = new IndexerNode(new MemberNode("a"), getterNode)
        {
            ItemType = Builtins.Int
        };
        var setter = new AssignNode(
            [indexer],
            [new LiteralNode(-1, Builtins.Int)]
        );

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int[]>()[1].ShouldBe(-1);
    }

    #endregion
}