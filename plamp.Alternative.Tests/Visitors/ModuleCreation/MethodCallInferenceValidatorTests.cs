using System;
using System.Reflection;
using System.Reflection.Emit;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModuleCreation;

public class MethodCallInferenceValidatorTests
{
    [Theory, AutoData]
    public void InferenceIntrinsic_ReturnsCorrect([Frozen] Mock<ITranslationTable> translationTable, MethodCallInferenceValidator visitor)
    {
        var symbolTable = new SymbolTable("mod1", []);
        symbolTable.TryAddFunc("println", RuntimeSymbols.GetSymbolTable.MakeVoid(), [RuntimeSymbols.GetSymbolTable.MakeAny()], default, out var fnRef);

        var methodInfo = typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(object)])!;
        
        fnRef.GetDefinitionInfo().SetClrMethod(methodInfo);
        var call = new CallNode(null, new FuncCallNameNode("println"), [new LiteralNode("aaa", RuntimeSymbols.GetSymbolTable.MakeString())]);
        call.SetInfo(fnRef);
        
        var ast = new BodyNode([call]);
        var context = CreateContext(translationTable, symbolTable);
        var result = visitor.Validate(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty());
        
        call.Symbol.ShouldNotBeNull();
        call.Symbol.GetDefinitionInfo().ClrMethod.ShouldBe(methodInfo);
    }
    
    [Theory, AutoData]
    public void InferenceFunction_ReturnsCorrect(
        [Frozen] Mock<ITranslationTable> translationTable,
        MethodCallInferenceValidator visitor)
    {
        var call = new CallNode(null, new FuncCallNameNode("Abc"), []);
        var symbolTable = new SymbolTable("mod", []);
        symbolTable.TryAddFunc("Abc", RuntimeSymbols.GetSymbolTable.MakeVoid(), [], default, out var fnRef);
        call.SetInfo(fnRef);
        var ast = new BodyNode([call]);
        var context = CreateContext(translationTable, symbolTable);
        var result = visitor.Validate(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty());
        call.Symbol.ShouldBe(fnRef);
    }
    
    [Theory, AutoData]
    public void InferenceFunctionNotExist_ReturnsNull(
        [Frozen] Mock<ITranslationTable> translationTable,
        MethodCallInferenceValidator visitor)
    {
        var call = new CallNode(null, new FuncCallNameNode("Abc"), []);
        var ast = new BodyNode([call]);
        var context = CreateContext(translationTable, new SymbolTable("mod", []));
        var result = visitor.Validate(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty());
        call.Symbol.ShouldBeNull();
    }
    
    private CreationContext CreateContext(Mock<ITranslationTable> translationTable, SymbolTable symbolTable)
    {
        var preCreationContext = new PreCreationContext(translationTable.Object, symbolTable);
        var asmName = new AssemblyName(Guid.NewGuid().ToString());
        var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule(asmName.Name!);
        var context = new CreationContext(asm, module, symbolTable, preCreationContext);
        return context;
    }
}