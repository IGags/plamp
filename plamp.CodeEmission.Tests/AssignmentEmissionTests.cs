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
        var cast = EmissionSetupHelper.CreateCastNode(memberType, RuntimeSymbols.SymbolTable.MakeAny(), new MemberNode(memberName));
        var call = EmissionSetupHelper.CreateCallNode(
            null, 
            EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), 
            [cast]);
        return call;
    }
    
    public static IEnumerable<object[]> EmitAssign_Correct_DataProvider()
    {
        var intType = new TypeNode(new TypeNameNode("int"));
        intType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeInt());
        
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
                    [new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt())]),
                SetupCallback("a", RuntimeSymbols.SymbolTable.MakeInt()),
                new ReturnNode(null)
            ]),
            new List<object>{1}
        ];
        
        var stringType = new TypeNode(new TypeNameNode("string"));
        stringType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeString());
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
                    [new LiteralNode("Hello", RuntimeSymbols.SymbolTable.MakeString()), new LiteralNode("World", RuntimeSymbols.SymbolTable.MakeString())]),
                SetupCallback("a", RuntimeSymbols.SymbolTable.MakeString()),
                SetupCallback("b", RuntimeSymbols.SymbolTable.MakeString()),
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
                    [new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()), new LiteralNode(2, RuntimeSymbols.SymbolTable.MakeInt())]),
                new AssignNode(
                    [new MemberNode("a"), new MemberNode("b")],
                    [new MemberNode("b"), new MemberNode("a")]),
                SetupCallback("a", RuntimeSymbols.SymbolTable.MakeInt()),
                SetupCallback("b", RuntimeSymbols.SymbolTable.MakeInt()),
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
        arrType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeString().MakeArrayType());

        var ixGetter1 = new IndexerNode(new MemberNode("arr"), new LiteralNode(0, RuntimeSymbols.SymbolTable.MakeInt()));
        ixGetter1.SetItemType(RuntimeSymbols.SymbolTable.MakeString());
        var ixGetter2 = new IndexerNode(new MemberNode("arr"), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()));
        ixGetter2.SetItemType(RuntimeSymbols.SymbolTable.MakeString());
        var ixGetter3 = new IndexerNode(new MemberNode("arr"), new LiteralNode(2, RuntimeSymbols.SymbolTable.MakeInt()));
        ixGetter3.SetItemType(RuntimeSymbols.SymbolTable.MakeString());
        
        var cast1 = EmissionSetupHelper.CreateCastNode(RuntimeSymbols.SymbolTable.MakeString().MakeArrayType(), RuntimeSymbols.SymbolTable.MakeAny(), ixGetter1);
        var cast2 = EmissionSetupHelper.CreateCastNode(RuntimeSymbols.SymbolTable.MakeString().MakeArrayType(), RuntimeSymbols.SymbolTable.MakeAny(), ixGetter2);
        var cast3 = EmissionSetupHelper.CreateCastNode(RuntimeSymbols.SymbolTable.MakeString().MakeArrayType(), RuntimeSymbols.SymbolTable.MakeAny(), ixGetter3);
        var call1 = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), [cast1]);
        var call2 = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), [cast2]);
        var call3 = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(typeof(AssignmentEmissionTests).GetMethod(nameof(Callback))!), [cast3]);

        var ixSetter1 = new IndexerNode(new MemberNode("arr"), new LiteralNode(0, RuntimeSymbols.SymbolTable.MakeInt()));
        ixSetter1.SetItemType(RuntimeSymbols.SymbolTable.MakeString());
        var ixSetter2 = new IndexerNode(new MemberNode("arr"), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()));
        ixSetter2.SetItemType(RuntimeSymbols.SymbolTable.MakeString());
        var ixSetter3 = new IndexerNode(new MemberNode("arr"), new LiteralNode(2, RuntimeSymbols.SymbolTable.MakeInt()));
        ixSetter3.SetItemType(RuntimeSymbols.SymbolTable.MakeString());
        yield return
        [
            new BodyNode(
            [
                new AssignNode(
                    [new VariableDefinitionNode(arrType, new VariableNameNode("arr"))],
                    [new InitArrayNode(stringType, new LiteralNode(3, RuntimeSymbols.SymbolTable.MakeInt()))]),
                new AssignNode(
                    [
                        ixSetter1,
                        ixSetter2,
                        ixSetter3
                    ],
                    [
                        new LiteralNode("tri", RuntimeSymbols.SymbolTable.MakeString()),
                        new LiteralNode("dva", RuntimeSymbols.SymbolTable.MakeString()),
                        new LiteralNode("odin", RuntimeSymbols.SymbolTable.MakeString())
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
        arrType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeInt().MakeArrayType());
        var type = new TypeNode(new TypeNameNode("int"));
        type.SetTypeRef(RuntimeSymbols.SymbolTable.MakeInt());
        var initArray = new InitArrayNode(type, new LiteralNode(0, RuntimeSymbols.SymbolTable.MakeInt()));
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
                SetupCallback("a", RuntimeSymbols.SymbolTable.MakeInt().MakeArrayType()),
                SetupCallback("b", RuntimeSymbols.SymbolTable.MakeInt().MakeArrayType()),
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