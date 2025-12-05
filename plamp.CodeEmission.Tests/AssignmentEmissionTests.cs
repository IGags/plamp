using plamp.Abstractions;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.Intrinsics;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class AssignmentEmissionTests
{
    private static readonly List<object> CallbackList = [];
    
#pragma warning disable xUnit1013
    public static void Callback(object obj) => CallbackList.Add(obj);
#pragma warning restore xUnit1013

    private static CallNode SetupCallback(string memberName, ICompileTimeType memberType)
    {
        var cast = EmissionSetupHelper.CreateCastNode(memberType, RuntimeSymbols.GetSymbolTable.MakeAny(), new MemberNode(memberName));
        var call = EmissionSetupHelper.CreateCallNode(
            null, 
            EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), 
            [cast]);
        return call;
    }
    
    public static IEnumerable<object[]> EmitAssign_Correct_DataProvider()
    {
        var intType = new TypeNode(new TypeNameNode("int"));
        intType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeInt());
        
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
                    [new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt())]),
                SetupCallback("a", RuntimeSymbols.GetSymbolTable.MakeInt()),
                new ReturnNode(null)
            ]),
            new List<object>{1}
        ];
        
        var stringType = new TypeNode(new TypeNameNode("string"));
        stringType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeString());
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
                    [new LiteralNode("Hello", RuntimeSymbols.GetSymbolTable.MakeString()), new LiteralNode("World", RuntimeSymbols.GetSymbolTable.MakeString())]),
                SetupCallback("a", RuntimeSymbols.GetSymbolTable.MakeString()),
                SetupCallback("b", RuntimeSymbols.GetSymbolTable.MakeString()),
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
                    [new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt()), new LiteralNode(2, RuntimeSymbols.GetSymbolTable.MakeInt())]),
                new AssignNode(
                    [new MemberNode("a"), new MemberNode("b")],
                    [new MemberNode("b"), new MemberNode("a")]),
                SetupCallback("a", RuntimeSymbols.GetSymbolTable.MakeInt()),
                SetupCallback("b", RuntimeSymbols.GetSymbolTable.MakeInt()),
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
        arrType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeString().MakeArrayType());

        var ixGetter1 = new IndexerNode(new MemberNode("arr"), new LiteralNode(0, RuntimeSymbols.GetSymbolTable.MakeInt()));
        ixGetter1.SetItemType(RuntimeSymbols.GetSymbolTable.MakeString());
        var ixGetter2 = new IndexerNode(new MemberNode("arr"), new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt()));
        ixGetter2.SetItemType(RuntimeSymbols.GetSymbolTable.MakeString());
        var ixGetter3 = new IndexerNode(new MemberNode("arr"), new LiteralNode(2, RuntimeSymbols.GetSymbolTable.MakeInt()));
        ixGetter3.SetItemType(RuntimeSymbols.GetSymbolTable.MakeString());
        
        var cast1 = EmissionSetupHelper.CreateCastNode(RuntimeSymbols.GetSymbolTable.MakeString().MakeArrayType(), RuntimeSymbols.GetSymbolTable.MakeAny(), ixGetter1);
        var cast2 = EmissionSetupHelper.CreateCastNode(RuntimeSymbols.GetSymbolTable.MakeString().MakeArrayType(), RuntimeSymbols.GetSymbolTable.MakeAny(), ixGetter2);
        var cast3 = EmissionSetupHelper.CreateCastNode(RuntimeSymbols.GetSymbolTable.MakeString().MakeArrayType(), RuntimeSymbols.GetSymbolTable.MakeAny(), ixGetter3);
        var call1 = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), [cast1]);
        var call2 = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), [cast2]);
        var call3 = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), [cast3]);

        var ixSetter1 = new IndexerNode(new MemberNode("arr"), new LiteralNode(0, RuntimeSymbols.GetSymbolTable.MakeInt()));
        ixSetter1.SetItemType(RuntimeSymbols.GetSymbolTable.MakeString());
        var ixSetter2 = new IndexerNode(new MemberNode("arr"), new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt()));
        ixSetter2.SetItemType(RuntimeSymbols.GetSymbolTable.MakeString());
        var ixSetter3 = new IndexerNode(new MemberNode("arr"), new LiteralNode(2, RuntimeSymbols.GetSymbolTable.MakeInt()));
        ixSetter3.SetItemType(RuntimeSymbols.GetSymbolTable.MakeString());
        yield return
        [
            new BodyNode(
            [
                new AssignNode(
                    [new VariableDefinitionNode(arrType, new VariableNameNode("arr"))],
                    [new InitArrayNode(stringType, new LiteralNode(3, RuntimeSymbols.GetSymbolTable.MakeInt()))]),
                new AssignNode(
                    [
                        ixSetter1,
                        ixSetter2,
                        ixSetter3
                    ],
                    [
                        new LiteralNode("tri", RuntimeSymbols.GetSymbolTable.MakeString()),
                        new LiteralNode("dva", RuntimeSymbols.GetSymbolTable.MakeString()),
                        new LiteralNode("odin", RuntimeSymbols.GetSymbolTable.MakeString())
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
        arrType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeInt().MakeArrayType());
        var type = new TypeNode(new TypeNameNode("int"));
        type.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeInt());
        var initArray = new InitArrayNode(type, new LiteralNode(0, RuntimeSymbols.GetSymbolTable.MakeInt()));
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
                    [initArray]),
                SetupCallback("a", RuntimeSymbols.GetSymbolTable.MakeInt().MakeArrayType()),
                SetupCallback("b", RuntimeSymbols.GetSymbolTable.MakeInt().MakeArrayType()),
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