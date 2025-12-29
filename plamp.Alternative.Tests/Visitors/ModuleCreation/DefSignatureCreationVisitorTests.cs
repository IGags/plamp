using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModuleCreation;

public class DefSignatureCreationVisitorTests
{
    public static IEnumerable<object[]> VisitNoArgs_ReturnNoException_DataProvider()
    {
        yield return [Builtins.Int];
        yield return [Builtins.Void];
    }

    [Theory]
    [MemberData(nameof(VisitNoArgs_ReturnNoException_DataProvider))]
    public void VisitNoArgs_ReturnNoException(
        ITypeInfo returnTypeObject)
    {
        var translationTable = new Fixture().Freeze<Mock<ITranslationTable>>();
        var visitor = new Fixture().Create<FuncCreatorValidator>();
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode(returnTypeObject.Name))
        {
            TypeInfo = returnTypeObject
        };
        
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [], 
            new BodyNode([]));

        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.Exceptions.ShouldBeEmpty();
        ast.Func.ShouldNotBeNull();
    }

    [Theory, AutoData]
    public void VisitWithArgs_ReturnsNoException(
        [Frozen]Mock<ITranslationTable> translationTable,
        FuncCreatorValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };

        var argType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        
        var arg = new ParameterNode(argType, new ParameterNameNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [arg],
            new BodyNode([]));

        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.Exceptions.ShouldBeEmpty();
        ast.Func.ShouldNotBeNull();
    }

    [Theory, AutoData]
    public void VisitWithUnknownReturnType_ReturnWithoutMethodSignature(
        [Frozen] Mock<ITranslationTable> translationTable,
        FuncCreatorValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode("void"));

        var argType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var arg = new ParameterNode(argType, new ParameterNameNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [arg],
            new BodyNode([]));
        
        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.Exceptions.ShouldBeEmpty();
        ast.Func.ShouldBeNull();
    }

    [Theory, AutoData]
    public void VisitWithUnknownArgType_ReturnWithoutMethodSignature(
        [Frozen] Mock<ITranslationTable> translationTable,
        FuncCreatorValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };

        var argType = new TypeNode(new TypeNameNode("int"));
        var arg = new ParameterNode(argType, new ParameterNameNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [arg],
            new BodyNode([]));
        
        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.Exceptions.ShouldBeEmpty();
        ast.Func.ShouldBeNull();
    }

    private CreationContext CreateContext(Mock<ITranslationTable> translationTable)
    {
        var preCreationContext = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var asmName = new AssemblyName(Guid.NewGuid().ToString());
        var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule(asmName.Name!);
        var context = new CreationContext(asm, module, preCreationContext);
        return context;
    }
}