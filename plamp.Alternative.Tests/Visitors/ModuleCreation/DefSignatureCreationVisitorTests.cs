using System;
using System.Reflection;
using System.Reflection.Emit;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModuleCreation;

public class DefSignatureCreationVisitorTests
{
    [Theory]
    [InlineData(typeof(void))]
    [InlineData(typeof(int))]
    public void VisitNoArgs_ReturnNoException(
        Type returnTypeObject)
    {
        var symbolTable = new Fixture().Freeze<Mock<ISymbolTable>>();
        var fileName = new Fixture().Create<string>();
        var visitor = new Fixture().Create<DefSignatureCreationValidator>();
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new MemberNode(returnTypeObject.Name));
        returnType.SetType(returnTypeObject);
        var ast = new FuncNode(
            returnType,
            new MemberNode(funcName),
            [], 
            new BodyNode([]));

        var context = CreateContext(fileName, symbolTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldHaveSingleItem(),
            x => x.Methods[0]
                .ShouldSatisfyAllConditions(
                    y => y.Name.ShouldBe(funcName),
                    y => y.ReturnType.ShouldBe(returnTypeObject)));
    }

    [Theory, AutoData]
    public void VisitWithArgs_ReturnsNoException([Frozen]Mock<ISymbolTable> symbolTable, string fileName, DefSignatureCreationValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new MemberNode("void"));
        returnType.SetType(typeof(void));

        var argType = new TypeNode(new MemberNode("int"));
        argType.SetType(typeof(int));
        var arg = new ParameterNode(argType, new MemberNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new MemberNode(funcName),
            [arg],
            new BodyNode([]));

        var context = CreateContext(fileName, symbolTable);
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
    public void VisitWithUnknownReturnType_ReturnWithoutMethodSignature([Frozen] Mock<ISymbolTable> symbolTable,
        string fileName, DefSignatureCreationValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new MemberNode("void"));

        var argType = new TypeNode(new MemberNode("int"));
        argType.SetType(typeof(int));
        var arg = new ParameterNode(argType, new MemberNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new MemberNode(funcName),
            [arg],
            new BodyNode([]));
        
        var context = CreateContext(fileName, symbolTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldBeEmpty());
    }

    [Theory, AutoData]
    public void VisitWithUnknownArgType_ReturnWithoutMethodSignature([Frozen] Mock<ISymbolTable> symbolTable,
        string fileName, DefSignatureCreationValidator visitor)
    {
        const string funcName = "TestFunc";
        var returnType = new TypeNode(new MemberNode("void"));
        returnType.SetType(typeof(void));
        
        var argType = new TypeNode(new MemberNode("int"));
        var arg = new ParameterNode(argType, new MemberNode("first"));
        
        var ast = new FuncNode(
            returnType,
            new MemberNode(funcName),
            [arg],
            new BodyNode([]));
        
        var context = CreateContext(fileName, symbolTable);
        var result = visitor.Validate(ast, context);
        //Cannot validate args due runtime constraints
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Methods.ShouldBeEmpty());
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