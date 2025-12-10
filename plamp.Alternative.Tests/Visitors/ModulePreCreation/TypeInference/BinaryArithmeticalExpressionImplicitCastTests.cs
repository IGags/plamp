using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class BinaryArithmeticalExpressionImplicitCastTests
{
    private static CastNode CreateCast(object inner, ICompileTimeType typeFrom, ICompileTimeType typeTo)
    {
        var castType = new TypeNode(new TypeNameNode(typeTo.TypeName));
        castType.SetTypeRef(typeTo);
        var cast = new CastNode(castType, new LiteralNode(inner, typeFrom));
        cast.SetFromType(typeFrom);
        return cast;
    }
    
    public static IEnumerable<object[]> CreateImplicitCastForArithmeticalBinaryExpression_Correct_DataProvider()
    {
        yield return
        [
            "10 * 11",
            new MulNode(new LiteralNode(10, RuntimeSymbols.SymbolTable.Int), new LiteralNode(11, RuntimeSymbols.SymbolTable.Int))
        ];
        yield return 
        [
            "10i + 1b", 
            new AddNode(new LiteralNode(10, RuntimeSymbols.SymbolTable.Int), CreateCast((byte)1, RuntimeSymbols.SymbolTable.Byte, RuntimeSymbols.SymbolTable.Int))
        ];
        yield return
        [
            "10ui + 10i",
            new AddNode(
                CreateCast((uint)10, RuntimeSymbols.SymbolTable.Uint, RuntimeSymbols.SymbolTable.Long),
                CreateCast(10, RuntimeSymbols.SymbolTable.Int, RuntimeSymbols.SymbolTable.Long))
        ];
        yield return
        [
            "10.1 / 3",
            new DivNode(
                new LiteralNode(10.1, RuntimeSymbols.SymbolTable.Double),
                CreateCast(3, RuntimeSymbols.SymbolTable.Int, RuntimeSymbols.SymbolTable.Double))
        ];
        yield return
        [
            "13f * 441.2",
            new MulNode(
                CreateCast(13f, RuntimeSymbols.SymbolTable.Float, RuntimeSymbols.SymbolTable.Double),
                new LiteralNode(441.2, RuntimeSymbols.SymbolTable.Double))
        ];
        yield return
        [
            "1b + 124ul",
            new AddNode(
                CreateCast((byte)1, RuntimeSymbols.SymbolTable.Byte, RuntimeSymbols.SymbolTable.Ulong),
                new LiteralNode((ulong)124, RuntimeSymbols.SymbolTable.Ulong))
        ];
    }
    
    [Theory]
    [MemberData(nameof(CreateImplicitCastForArithmeticalBinaryExpression_Correct_DataProvider))]
    public void CreateImplicitCastForArithmeticalBinaryExpression_Correct(string code, NodeBase astShould)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParsePrecedence(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var resContext = visitor.WeaveDiffs(expression!, preCreation);
        resContext.Exceptions.ShouldBeEmpty();
        expression.ShouldBeEquivalentTo(astShould);
    }

    [Fact]
    public void AddLongAndUlong_CannotExpand()
    {
        const string code = "1l + 1ul";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParsePrecedence(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var resContext = visitor.WeaveDiffs(expression!, preCreation);
        var exception = PlampExceptionInfo.CannotApplyOperator().Code;
        resContext.Exceptions.Select(x => x.Code).ShouldContain(exception);
    }

    [Fact]
    public void AddStringToInt_CannotExpand()
    {
        const string code = "1 + \"1\"";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParsePrecedence(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var resContext = visitor.WeaveDiffs(expression!, preCreation);
        var exception = PlampExceptionInfo.CannotApplyOperator().Code;
        resContext.Exceptions.Select(x => x.Code).ShouldContain(exception);
    }
}