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

public class InitArrayEmissionTests
{
     public struct TestStruct;
     
     [Theory]
     [InlineData(typeof(ulong))]
     [InlineData(typeof(byte))]
     [InlineData(typeof(int))]
     [InlineData(typeof(uint))]
     [InlineData(typeof(long))]
     [InlineData(typeof(double))]
     [InlineData(typeof(float))]
     [InlineData(typeof(bool))]
     [InlineData(typeof(object))]
     [InlineData(typeof(string))]
     [InlineData(typeof(char))]
     [InlineData(typeof(TestStruct))]
     public async Task InitArrayOfType_ReturnsCorrect(Type arrayItemType)
     {
          /*
           * return [4]type;
           */
          var itemType = new TypeNode(new TypeNameNode(arrayItemType.Name));
          itemType.SetType(arrayItemType);

          var arrayInit = new InitArrayNode(itemType, new LiteralNode(4, typeof(int)));
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var arrayType = arrayItemType.MakeArrayType();
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, arrayType);
          var result = method!.Invoke(instance, []);
          
          result.ShouldNotBeNull().ShouldBeOfType(arrayType);
          var array = (Array)result;
          array.Length.ShouldBe(4);
     }

     [Fact]
     public async Task InitArrayOfZeroLiteralLength_ReturnsCorrect()
     {
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var arrayInit = new InitArrayNode(itemType, new LiteralNode(0, typeof(int)));
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int[]));
          var result = method!.Invoke(instance, []);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(0);
     }

     [Fact]
     public async Task InitArrayOfNegativeLength_ThrowsRuntimeException()
     {
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var arrayInit = new InitArrayNode(itemType, new LiteralNode(-1, typeof(int)));
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int[]));
          Should.Throw<TargetInvocationException>(() => method!.Invoke(instance, []))
               .InnerException.ShouldBeOfType<OverflowException>();
     }

     [Fact]
     public async Task InitArrayWithParametrizedLength_ReturnsCorrect()
     {
          /*
           * fn mk_arr(int length) []int { return [length]int; }
           */
          var parameter = new TestParameter(typeof(int), "length");
          
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var arrayInit = new InitArrayNode(itemType, new MemberNode("length"));
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([parameter], body, typeof(int[]));

          var length = Random.Shared.Next(0, 100);
          var result = method!.Invoke(instance, [length]);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(length);
     }

     [Fact]
     public async Task InitArrayWithUnaryOperatorLength_ReturnsCorrect()
     {
          /*
           * fn mk_arr(int length) []int { return [length++]int; }
           */
          var parameter = new TestParameter(typeof(int), "length");
          
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var arrayInit = new InitArrayNode(itemType, new PostfixIncrementNode(new MemberNode("length")));
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([parameter], body, typeof(int[]));

          const int length = 1;
          var result = method!.Invoke(instance, [length]);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(length);
     }

     [Fact]
     public async Task InitArrayWithBinaryOperatorLength_ReturnsCorrect()
     {
          /*
           * fn mk_arr() []int { return [4 - 3]int; }
           */
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var arrayInit = new InitArrayNode(itemType, new SubNode(new LiteralNode(4, typeof(int)), new LiteralNode(3, typeof(int))));
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int[]));

          var result = method!.Invoke(instance, []);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(1);
     }

     [Fact]
     public async Task InitArrayWithCastOperatorLength_ReturnsCorrect()
     {
          /*
           * fn mk_arr() []int { return [int(4.0)]int; }
           */
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var cast = new CastNode(itemType, new LiteralNode(4.0, typeof(double)));
          cast.SetFromType(typeof(double));
          
          var arrayInit = new InitArrayNode(itemType, cast);
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int[]));

          var result = method!.Invoke(instance, []);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(4);
     }

     [Fact]
     public async Task InitArrayWithFuncCallLength_ReturnsCorrect()
     {
          /*
           * fn mk_arr() []int { return [getLength()]int; }
           */
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var call = new CallNode(null, new FuncCallNameNode(nameof(GetLength)), []);
          call.SetInfo(typeof(InitArrayEmissionTests).GetMethod(nameof(GetLength))!);
          var arrayInit = new InitArrayNode(itemType, call);
          var body = new BodyNode([new ReturnNode(arrayInit)]);
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int[]));

          var result = method!.Invoke(instance, []);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(15);
     }

     public static int GetLength() => 15;

     [Fact]
     public async Task InitArrayWithArrayGetterLength_ReturnsCorrect()
     {
          /*
           * fn mk_arr() []int {
           *   a := [1]int;
           *   a[0] := 5;
           *   return [a[0]]int;
           * }
           */
          var itemType = new TypeNode(new TypeNameNode(nameof(Int32)));
          itemType.SetType(typeof(int));

          var arrayType = new TypeNode(new TypeNameNode("[]int"));
          arrayType.SetType(typeof(int[]));
          
          var arrayInit = new InitArrayNode(itemType, new LiteralNode(1, typeof(int)));
          var assign = new AssignNode(new VariableDefinitionNode(arrayType, new VariableNameNode("a")), arrayInit);

          var setItem = new ElemSetterNode(
               new MemberNode("a"), 
               new ArrayIndexerNode(new LiteralNode(0, typeof(int))),
               new LiteralNode(5, typeof(int)));
          setItem.SetItemType(typeof(int));
          
          var getter = new ElemGetterNode(new MemberNode("a"), new ArrayIndexerNode(new LiteralNode(0, typeof(int))));
          getter.SetItemType(typeof(int));
          var returnArrayInit = new InitArrayNode(itemType, getter);
          
          var body = new BodyNode(
          [
               assign,
               setItem,
               new ReturnNode(returnArrayInit)
          ]);
          
          var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(int[]));

          var result = method!.Invoke(instance, []);
          
          result.ShouldNotBeNull().ShouldBeOfType(typeof(int[]));
          var array = (Array)result;
          array.Length.ShouldBe(5);
     }
}