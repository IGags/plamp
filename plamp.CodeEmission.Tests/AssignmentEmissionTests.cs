using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.CodeEmission.Tests.Infrastructure;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class AssignmentEmissionTests
{
    private static readonly List<object> CallbackList = [];
    
#pragma warning disable xUnit1013
    public static void Callback(object obj) => CallbackList.Add(obj);
#pragma warning restore xUnit1013

    private static CallNode SetupCallback(string memberName, Type memberType)
    {
        var cast = EmissionSetupHelper.CreateCastNode(memberType, typeof(object), new MemberNode(memberName));
        var call = EmissionSetupHelper.CreateCallNode(
            null, 
            typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!, 
            [cast]);
        return call;
    }
    
    public static IEnumerable<object[]> EmitAssign_Correct_DataProvider()
    {
        var intType = new TypeNode(new TypeNameNode("int"));
        intType.SetTypeRef(typeof(int));
        
        /*
         * a := 1;
         * callback(a);
         */
        yield return
        [
            new BodyNode(
            [
                new VariableDefinitionNode(intType, new VariableNameNode("a")),
                new AssignNode(
                    [new MemberNode("a")],
                    [new LiteralNode(1, typeof(int))]),
                SetupCallback("a", typeof(int)),
                new ReturnNode(null)
            ]),
            new List<object>{1}
        ];
        
        var stringType = new TypeNode(new TypeNameNode("string"));
        stringType.SetTypeRef(typeof(string));
        /*
         * a, b := "Hello", "World";
         * callback(a);
         * callback(b);
         */
        yield return 
        [
            new BodyNode(
            [
                new VariableDefinitionNode(stringType, [new VariableNameNode("a"), new VariableNameNode("b")]),
                new AssignNode(
                    [new MemberNode("a"), new MemberNode("b")],
                    [new LiteralNode("Hello", typeof(string)), new LiteralNode("World", typeof(string))]),
                SetupCallback("a", typeof(string)),
                SetupCallback("b", typeof(string)),
                new ReturnNode(null)
            ]),
            new List<object>{"Hello", "World"}
        ];
        
        /*
         * int a, b;
         * a, b := 1, 2;
         * b, a := a, b;
         * callback(a);
         * callback(b);
         */
        yield return
        [
            new BodyNode(
            [
                new VariableDefinitionNode(intType, [new VariableNameNode("a"), new VariableNameNode("b")]),
                new AssignNode(
                    [new MemberNode("a"), new MemberNode("b")],
                    [new LiteralNode(1, typeof(int)), new LiteralNode(2, typeof(int))]),
                new AssignNode(
                    [new MemberNode("a"), new MemberNode("b")],
                    [new MemberNode("b"), new MemberNode("a")]),
                SetupCallback("a", typeof(int)),
                SetupCallback("b", typeof(int)),
                new ReturnNode(null)
            ]),
            new List<object>{2, 1}
        ];

        /*
         * arr := [3]string;
         * arr[0], arr[1], arr[2] := "tri", "dva", "odin";
         * callback(arr[0]);
         * callback(arr[1]);
         * callback(arr[2]);
         */
        var arrType = new TypeNode(new TypeNameNode("[]string")) { ArrayDefinitions = [new ArrayTypeSpecificationNode()] };
        arrType.SetTypeRef(typeof(string[]));

        var ixGetter1 = new IndexerNode(new MemberNode("arr"), new LiteralNode(0, typeof(int)));
        ixGetter1.SetItemType(typeof(string));
        var ixGetter2 = new IndexerNode(new MemberNode("arr"), new LiteralNode(1, typeof(int)));
        ixGetter2.SetItemType(typeof(string));
        var ixGetter3 = new IndexerNode(new MemberNode("arr"), new LiteralNode(2, typeof(int)));
        ixGetter3.SetItemType(typeof(string));
        
        var cast1 = EmissionSetupHelper.CreateCastNode(typeof(string[]), typeof(object), ixGetter1);
        var cast2 = EmissionSetupHelper.CreateCastNode(typeof(string[]), typeof(object), ixGetter2);
        var cast3 = EmissionSetupHelper.CreateCastNode(typeof(string[]), typeof(object), ixGetter3);
        var call1 = EmissionSetupHelper.CreateCallNode(null, typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!, [cast1]);
        var call2 = EmissionSetupHelper.CreateCallNode(null, typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!, [cast2]);
        var call3 = EmissionSetupHelper.CreateCallNode(null, typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!, [cast3]);

        var ixSetter1 = new IndexerNode(new MemberNode("arr"), new LiteralNode(0, typeof(int)));
        ixSetter1.SetItemType(typeof(string));
        var ixSetter2 = new IndexerNode(new MemberNode("arr"), new LiteralNode(1, typeof(int)));
        ixSetter2.SetItemType(typeof(string));
        var ixSetter3 = new IndexerNode(new MemberNode("arr"), new LiteralNode(2, typeof(int)));
        ixSetter3.SetItemType(typeof(string));
        yield return
        [
            new BodyNode(
            [
                new AssignNode(
                    [new VariableDefinitionNode(arrType, new VariableNameNode("arr"))],
                    [new InitArrayNode(stringType, new LiteralNode(3, typeof(int)))]),
                new AssignNode(
                    [
                        ixSetter1,
                        ixSetter2,
                        ixSetter3
                    ],
                    [
                        new LiteralNode("tri", typeof(string)),
                        new LiteralNode("dva", typeof(string)),
                        new LiteralNode("odin", typeof(string))
                    ]),
                call1,
                call2,
                call3,
                new ReturnNode(null)
            ]),
            new List<object>{"tri", "dva", "odin"}
        ];

        arrType = new TypeNode(new TypeNameNode("[]int"))
            { ArrayDefinitions = [new ArrayTypeSpecificationNode()] };
        arrType.SetTypeRef(typeof(int[]));
        var info = typeof(Array).GetMethod(nameof(Array.Empty))!;
        info = info.MakeGenericMethod(typeof(int));
        /*
         * []int a, b;
         * callback(a);
         * callback(b);
         */
        yield return
        [
            new BodyNode(
            [
                new AssignNode(
                    [new VariableDefinitionNode(arrType, [new VariableNameNode("a"), new VariableNameNode("b")])],
                    [EmissionSetupHelper.CreateCallNode(null, info, [])]),
                SetupCallback("a", typeof(int[])),
                SetupCallback("b", typeof(int[])),
                new ReturnNode(null)
            ]),
            new List<object> { Array.Empty<int>(), Array.Empty<int>() }
        ];
    }
    
    [Theory]
    [MemberData(nameof(EmitAssign_Correct_DataProvider))]
    public void EmitAssign_Correct(BodyNode code, List<object> assignResCorrect)
    {
        var (instance, method) = EmissionSetupHelper.CreateInstanceWithMethod([], code, typeof(void));
        CallbackList.Clear();
        method!.Invoke(instance, []);
        CallbackList.ShouldBeEquivalentTo(assignResCorrect);
    }
}