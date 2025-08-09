using System.Collections.Generic;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class FuncSignatureInferenceVisitorTests
{
    [Theory, AutoData]
    public void EmptyRoot_NoExceptionNoSignatures([Frozen]Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var root = new RootNode([], null, []);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(root, context);
        Assert.Empty(result.Functions);
        Assert.Empty(result.Exceptions);
    }

    [Theory, AutoData]
    public void VoidDefinitionEmptyArgs_SingleSignature([Frozen]Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        const string funcName = "fn1";
        var ast = new RootNode(
            [],
            null,
            [
                new FuncNode(
                    null, new MemberNode(funcName), [], 
                    new BodyNode([]))
            ]);

        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, context);
        
        result.Functions.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Key.ShouldBe(funcName),
                x => x.Value.ShouldSatisfyAllConditions(
                    y => y.ShouldBe(ast.Functions[0]),
                    y => y.ReturnType.ShouldNotBeNull()
                        .Symbol.ShouldBe(typeof(void))));
    }

    [Theory, AutoData]
    public void IntDefinitionEmptyArgs_SingleSignature([Frozen]Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        const string funcName = "fn1";
        var ast = new RootNode(
            [],
            null,
            [
                new FuncNode(
                    new TypeNode(new MemberNode("int")), 
                    new MemberNode(funcName), [], 
                    new BodyNode([]))
            ]);
        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, context);
        
        result.Functions.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Key.ShouldBe(funcName),
                x => x.Value.ShouldSatisfyAllConditions(
                    y => y.ShouldBe(ast.Functions[0]),
                    y => y.ReturnType.ShouldNotBeNull()
                        .Symbol.ShouldBe(typeof(int))));
    }

    [Theory, AutoData]
    public void VoidDefinitionOneArg_SingleSignature([Frozen]Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        const string funcName = "fn1";
        var ast = new RootNode(
            [],
            null,
            [
                new FuncNode(
                    null, 
                    new MemberNode(funcName), 
                    [
                        new ParameterNode(new TypeNode(new MemberNode("int")), new MemberNode("one"))
                    ], 
                    new BodyNode([]))
            ]);
        
        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, context);
        
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Functions.ShouldHaveSingleItem()
                .ShouldSatisfyAllConditions(
                    y => y.Key.ShouldBe(funcName),
                    y => y.Value.ShouldSatisfyAllConditions(
                        z => z.ShouldBe(ast.Functions[0]),
                        z => z.ReturnType.ShouldNotBeNull().Symbol.ShouldBe(typeof(void)),
                        z => z.ParameterList.ShouldHaveSingleItem()
                            .Type.ShouldNotBeNull().Symbol.ShouldBe(typeof(int)))));
    }

    [Theory, AutoData]
    public void VoidDefinitionManyArgs_SingleSignature([Frozen]Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        const string funcName = "fn1";
        var ast = new RootNode(
            [],
            null,
            [
                new FuncNode(
                    null, 
                    new MemberNode(funcName), 
                    [
                        new ParameterNode(new TypeNode(new MemberNode("int")), new MemberNode("one")), 
                        new ParameterNode(new TypeNode(new MemberNode("string")), new MemberNode("two")), 
                    ], 
                    new BodyNode([]))
            ]);
        
        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, context);

        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Functions.ShouldHaveSingleItem()
                .ShouldSatisfyAllConditions(
                    y => y.Key.ShouldBe(funcName),
                    y => y.Value.ShouldSatisfyAllConditions(
                        z => z.ShouldBe(ast.Functions[0]),
                        z => z.ReturnType.ShouldNotBeNull().Symbol.ShouldBe(typeof(void)),
                        z => z.ParameterList.ShouldSatisfyAllConditions(
                            w => w[0].Type.ShouldNotBeNull().Symbol.ShouldBe(typeof(int)),
                            w => w[1].Type.ShouldNotBeNull().Symbol.ShouldBe(typeof(string))
                        )
                    )
                )
        );
    }

    [Theory, AutoData]
    public void ManyDefinitionsDifferentNames_ReturnManySignatures([Frozen]Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        const string funcName = "fn1";
        const string funcName2 = "fn2";
        
        var ast = new RootNode(
            [],
            null,
            [
                new FuncNode(null, new MemberNode(funcName), [], new BodyNode([])),
                new FuncNode(null, new MemberNode(funcName2), [], new BodyNode([]))
            ]);
        
        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, context);

        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Functions
                .ShouldSatisfyAllConditions(
                    y => y.ShouldContainKey(funcName),
                    y => y[funcName].ShouldSatisfyAllConditions(
                        z => z.ShouldBe(ast.Functions[0]),
                        z => z.ReturnType.ShouldNotBeNull().Symbol.ShouldBe(typeof(void)),
                        z => z.ParameterList.ShouldBeEmpty()
                    ),
                    y => y.ShouldContainKey(funcName2),
                    y => y[funcName2].ShouldSatisfyAllConditions(
                        z => z.ShouldBe(ast.Functions[1]),
                        z => z.ReturnType.ShouldNotBeNull().Symbol.ShouldBe(typeof(void)),
                        z => z.ParameterList.ShouldBeEmpty()
                    )
                )
        );
    }

    [Theory, AutoData]
    public void ManyDefinitionsSameNames_ReturnEmptySignatures([Frozen] Mock<ISymbolTable> symbolTableMock, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        const string funcName = "fn1";
        
        var ast = new RootNode(
            [],
            null,
            [
                new FuncNode(null, new MemberNode(funcName), [], new BodyNode([])),
                new FuncNode(null, new MemberNode(funcName), [], new BodyNode([]))
            ]);
        
        var context = new PreCreationContext(fileName, symbolTableMock.Object);
        var visitor = new SignatureTypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldBeEmpty(),
            x => x.Functions.ShouldBeEmpty());
    }
}