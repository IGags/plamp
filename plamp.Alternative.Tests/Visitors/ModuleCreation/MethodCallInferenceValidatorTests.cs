using System;
using System.Reflection;
using System.Reflection.Emit;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModuleCreation;

public class MethodCallInferenceValidatorTests
{
    [Theory, AutoData]
    public void InferenceIntrinsic_ReturnsCorrect([Frozen] Mock<ISymbolTable> symbolTable, string fileName, MethodCallInferenceValidator visitor)
    {
        var call = new CallNode(null, new MemberNode("println"), [new LiteralNode("aaa", typeof(string))]);
        var ast = new BodyNode([call]);
        var context = CreateContext(fileName, symbolTable);
        var result = visitor.Validate(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty());
        call.Symbol.ShouldBe(typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(object)]));
    }
    
    [Theory, AutoData]
    public void InferenceFunction_ReturnsCorrect([Frozen] Mock<ISymbolTable> symbolTable, string fileName,
        MethodCallInferenceValidator visitor)
    {
        var call = new CallNode(null, new MemberNode("Abc"), []);
        var ast = new BodyNode([call]);
        var context = CreateContext(fileName, symbolTable);
        var method = context.ModuleBuilder.DefineGlobalMethod("Abc", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, typeof(void), []);
        context.Methods.Add(method);
        var result = visitor.Validate(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty());
        call.Symbol.ShouldBe(method);
    }

    [Theory, AutoData]
    public void InferenceFunctionNotExist_ReturnsNull([Frozen] Mock<ISymbolTable> symbolTable, string fileName,
        MethodCallInferenceValidator visitor)
    {
        var call = new CallNode(null, new MemberNode("Abc"), []);
        var ast = new BodyNode([call]);
        var context = CreateContext(fileName, symbolTable);
        var result = visitor.Validate(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty());
        call.Symbol.ShouldBeNull();
    }
    
    private CreationContext CreateContext(string fileName, Mock<ISymbolTable> symbolTable)
    {
        var preCreationContext = new PreCreationContext(fileName, symbolTable.Object);
        var asmName = new AssemblyName(Guid.NewGuid().ToString());
        var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule(asmName.Name!);
        var context = new CreationContext(asm, module, preCreationContext);
        return context;
    }
}