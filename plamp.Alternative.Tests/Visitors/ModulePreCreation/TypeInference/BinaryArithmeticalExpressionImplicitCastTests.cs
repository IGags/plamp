using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class BinaryArithmeticalExpressionImplicitCastTests
{
    private static CastNode CreateCast(object inner, Type typeFrom, Type typeTo)
    {
        var castType = new TypeNode(new TypeNameNode(typeTo.Name));
        castType.SetType(typeTo);
        var cast = new CastNode(castType, new LiteralNode(inner, typeFrom));
        cast.SetFromType(typeFrom);
        return cast;
    }
    
    public static IEnumerable<object[]> CreateImplicitCastForArithmeticalBinaryExpression_Correct_DataProvider()
    {
        yield return
        [
            "10 * 11",
            new MulNode(new LiteralNode(10, typeof(int)), new LiteralNode(11, typeof(int)))
        ];
        yield return 
        [
            "10i + 1b", 
            new AddNode(new LiteralNode(10, typeof(int)), CreateCast((byte)1, typeof(byte), typeof(int)))
        ];
        yield return
        [
            "10ui + 10i",
            new AddNode(
                CreateCast((uint)10, typeof(uint), typeof(long)),
                CreateCast(10, typeof(int), typeof(long)))
        ];
        yield return
        [
            "10.1 / 3",
            new DivNode(
                new LiteralNode(10.1, typeof(double)),
                CreateCast(3, typeof(int), typeof(double)))
        ];
        yield return
        [
            "13f * 441.2",
            new MulNode(
                CreateCast(13f, typeof(float), typeof(double)),
                new LiteralNode(441.2, typeof(double)))
        ];
        yield return
        [
            "1b + 124ul",
            new AddNode(
                CreateCast((byte)1, typeof(byte), typeof(ulong)),
                new LiteralNode((ulong)124, typeof(ulong)))
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
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
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
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
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
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var resContext = visitor.WeaveDiffs(expression!, preCreation);
        var exception = PlampExceptionInfo.CannotApplyOperator().Code;
        resContext.Exceptions.Select(x => x.Code).ShouldContain(exception);
    }
}