using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModuleCreation;

public class DefSignatureCreationVisitorTests
{
    public static IEnumerable<object[]> VisitNoArgs_ReturnNoException_DataProvider()
    {
        yield return [RuntimeSymbols.SymbolTable.Int];
        yield return [RuntimeSymbols.SymbolTable.Void];
    }

    [Theory]
    [MemberData(nameof(VisitNoArgs_ReturnNoException_DataProvider))]
    public void VisitNoArgs_ReturnNoException(
        ICompileTimeType returnTypeObject)
    {
        var translationTable = new Fixture().Freeze<Mock<ITranslationTable>>();
        var visitor = new Fixture().Create<DefSignatureCreationValidator>();
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode(returnTypeObject.GetDefinitionInfo().TypeName));
        returnType.SetTypeRef(returnTypeObject);
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [], 
            new BodyNode([]));

        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldHaveSingleItem(),
            x => x.Methods[0]
                .ShouldSatisfyAllConditions(
                    y => y.Name.ShouldBe(funcName),
                    y => y.ReturnType.ShouldBe(returnTypeObject.GetDefinitionInfo().ClrType!)));
    }

    [Theory, AutoData]
    public void VisitWithArgs_ReturnsNoException(
        [Frozen]Mock<ITranslationTable> translationTable,
        DefSignatureCreationValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode("void"));
        returnType.SetTypeRef(RuntimeSymbols.SymbolTable.Void);

        var argType = new TypeNode(new TypeNameNode("int"));
        argType.SetTypeRef(RuntimeSymbols.SymbolTable.Int);
        var arg = new ParameterNode(argType, new ParameterNameNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [arg],
            new BodyNode([]));

        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldHaveSingleItem(),
            x => x.Methods[0]
                .ShouldSatisfyAllConditions(
                    y => y.Name.ShouldBe(funcName),
                    y => y.ReturnType.ShouldBe(typeof(void))));
    }

    [Theory, AutoData]
    public void VisitWithUnknownReturnType_ReturnWithoutMethodSignature(
        [Frozen] Mock<ITranslationTable> translationTable,
        DefSignatureCreationValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode("void"));

        var argType = new TypeNode(new TypeNameNode("int"));
        argType.SetTypeRef(RuntimeSymbols.SymbolTable.Int);
        var arg = new ParameterNode(argType, new ParameterNameNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new FuncNameNode(funcName),
            [arg],
            new BodyNode([]));
        
        var context = CreateContext(translationTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldBeEmpty());
    }

    [Theory, AutoData]
    public void VisitWithUnknownArgType_ReturnWithoutMethodSignature(
        [Frozen] Mock<ITranslationTable> translationTable,
        DefSignatureCreationValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new TypeNameNode("void"));
        returnType.SetTypeRef(RuntimeSymbols.SymbolTable.Void);
        
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
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldBeEmpty());
    }

    private CreationContext CreateContext(Mock<ITranslationTable> translationTable)
    {
        var preCreationContext = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var currentModule = (SymbolTable)preCreationContext.Dependencies.First(x => x != RuntimeSymbols.SymbolTable);
        var asmName = new AssemblyName(Guid.NewGuid().ToString());
        var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule(asmName.Name!);
        var context = new CreationContext(asm, module, currentModule, preCreationContext);
        return context;
    }
}