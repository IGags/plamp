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
        var arrayItemType = new TypeNode(new TypeNameNode("int"));
        arrayItemType.SetType(typeof(int));

        var arrayType = new TypeNode(new TypeNameNode("[]int"));
        arrayType.SetType(typeof(int[]));
        
        var assign = new AssignNode(
            new VariableDefinitionNode(arrayType, new VariableNameNode("a")),
            new InitArrayNode(arrayItemType, new LiteralNode(3, typeof(int))));

        var literal1 = new LiteralNode(1, typeof(int));
        var setter1 = new ElemSetterNode(new MemberNode("a"), new IndexerNode(literal1), literal1);
        setter1.SetItemType(typeof(int));
        
        var literal2 = new LiteralNode(2, typeof(int));
        var setter2 = new ElemSetterNode(new MemberNode("a"), new IndexerNode(literal2), literal2);
        setter2.SetItemType(typeof(int));
        
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
        var elemGetter = new ElemGetterNode(new MemberNode("a"), new IndexerNode(new LiteralNode(index, typeof(int))));
        elemGetter.SetItemType(typeof(int));

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
        var elemGetter = new ElemGetterNode(new MemberNode("a"), new IndexerNode(new LiteralNode(-1, typeof(int))));
        elemGetter.SetItemType(typeof(int));

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
        var elemGetter = new ElemGetterNode(new MemberNode("a"), new IndexerNode(new LiteralNode(4, typeof(int))));
        elemGetter.SetItemType(typeof(int));

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
        var variableType = new TypeNode(new TypeNameNode("int"));
        variableType.SetType(typeof(int));

        var definition = new VariableDefinitionNode(variableType, new VariableNameNode("i"));
        var assign = new AssignNode(definition, new LiteralNode(0, typeof(int)));
        
        var elemGetter = new ElemGetterNode(
            new MemberNode("a"), 
            new IndexerNode(new PostfixIncrementNode(new MemberNode("i"))));
        elemGetter.SetItemType(typeof(int));
        
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
        var elemGetter = new ElemGetterNode(
            new MemberNode("a"), 
            new IndexerNode(new AddNode(new LiteralNode(1, typeof(int)), new LiteralNode(1, typeof(int)))));
        elemGetter.SetItemType(typeof(int));
        
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
        var castTargetType = new TypeNode(new TypeNameNode("int"));
        castTargetType.SetType(typeof(int));

        var cast = new CastNode(castTargetType, new LiteralNode(1.0, typeof(double)));
        cast.SetFromType(typeof(double));
        
        var elemGetter = new ElemGetterNode(new MemberNode("a"), new IndexerNode(cast));
        elemGetter.SetItemType(typeof(int));
        
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
        call.SetInfo(typeof(ArrayElementManipulation).GetMethod(nameof(GetZero))!);
        
        var elemGetter = new ElemGetterNode(new MemberNode("a"), new IndexerNode(call));
        elemGetter.SetItemType(typeof(int));
        
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
        var elemGetterInner = new ElemGetterNode(new MemberNode("a"), new IndexerNode(new LiteralNode(1, typeof(int))));
        elemGetterInner.SetItemType(typeof(int));
        
        var elemGetter = new ElemGetterNode(new MemberNode("a"), new IndexerNode(elemGetterInner));
        elemGetter.SetItemType(typeof(int));
        
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
         * a[ix] = -ix;
         * return a;
         */
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(new MemberNode("ix")),
            new UnaryMinusNode(new MemberNode("ix")));
        setter.SetItemType(typeof(int));

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
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(new LiteralNode(-1, typeof(int))),
            new LiteralNode(-1, typeof(int)));
        setter.SetItemType(typeof(int));

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
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(new LiteralNode(4, typeof(int))),
            new LiteralNode(0, typeof(int)));
        setter.SetItemType(typeof(int));

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
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(new UnaryMinusNode(new LiteralNode(-1, typeof(int)))),
            new LiteralNode(42, typeof(int)));
        setter.SetItemType(typeof(int));

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
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(new SubNode(new LiteralNode(100, typeof(int)), new LiteralNode(99, typeof(int)))),
            new LiteralNode(98, typeof(int)));
        setter.SetItemType(typeof(int));

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
        var toType = new TypeNode(new TypeNameNode("int"));
        toType.SetType(typeof(int));
        var cast = new CastNode(toType, new LiteralNode(2.0, typeof(double)));
        cast.SetFromType(typeof(double));
        
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(cast),
            new LiteralNode(11, typeof(int)));
        setter.SetItemType(typeof(int));

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
        callNode.SetInfo(typeof(ArrayElementManipulation).GetMethod(nameof(GetZero))!);
        
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(callNode),
            new LiteralNode(-99, typeof(int)));
        setter.SetItemType(typeof(int));

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
        var getterNode = new ElemGetterNode(new MemberNode("a"), new IndexerNode(new LiteralNode(1, typeof(int))));
        getterNode.SetItemType(typeof(int));
        
        var setter = new ElemSetterNode(
            new MemberNode("a"), 
            new IndexerNode(getterNode),
            new LiteralNode(-1, typeof(int)));
        setter.SetItemType(typeof(int));

        var instructionList = MakeArrayInitAst();
        instructionList.AddRange([setter, new ReturnNode(new MemberNode("a"))]);
        var body = new BodyNode(instructionList);

        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int[]));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int[]>()[1].ShouldBe(-1);
    }

    #endregion
}