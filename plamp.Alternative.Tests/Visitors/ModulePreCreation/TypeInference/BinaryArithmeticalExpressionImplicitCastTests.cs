using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class BinaryArithmeticalExpressionImplicitCastTests
{
    private static CastNode CreateCast(object inner, ITypeInfo typeFrom, ITypeInfo typeTo)
    {
        var castType = new TypeNode(new TypeNameNode(typeTo.Name))
        {
            TypeInfo = typeTo
        };
        var cast = new CastNode(castType, new LiteralNode(inner, typeFrom))
        {
            FromType = typeFrom
        };
        return cast;
    }
    
    public static IEnumerable<object[]> CreateImplicitCastForArithmeticalBinaryExpression_Correct_DataProvider()
    {
        yield return
        [
            "10 * 11",
            new MulNode(new LiteralNode(10, Builtins.Int), new LiteralNode(11, Builtins.Int))
        ];
        yield return 
        [
            "10i + 1b", 
            new AddNode(new LiteralNode(10, Builtins.Int), CreateCast((byte)1, Builtins.Byte, Builtins.Int))
        ];
        yield return
        [
            "10ui + 10i",
            new AddNode(
                CreateCast((uint)10, Builtins.Uint, Builtins.Long),
                CreateCast(10, Builtins.Int, Builtins.Long))
        ];
        yield return
        [
            "10.1 / 3",
            new DivNode(
                new LiteralNode(10.1, Builtins.Double),
                CreateCast(3, Builtins.Int, Builtins.Double))
        ];
        yield return
        [
            "13f * 441.2",
            new MulNode(
                CreateCast(13f, Builtins.Float, Builtins.Double),
                new LiteralNode(441.2, Builtins.Double))
        ];
        yield return
        [
            "1b + 124ul",
            new AddNode(
                CreateCast((byte)1, Builtins.Byte, Builtins.Ulong),
                new LiteralNode((ulong)124, Builtins.Ulong))
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