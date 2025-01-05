using System.Linq;
using plamp.Ast.Node;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests.Parser;

#pragma warning disable CS0618
public class TypeParsingTests
{
    [Fact]
    public void ParseSimpleType()
    {
        var code = "int";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("int"), []);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(0, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseSimpleGenericType()
    {
        var code = "List<int>";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), [new TypeNode(new MemberNode("int"), [])]);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(3, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseNotClosedGeneric()
    {
        var code = "List<int";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), [new TypeNode(new MemberNode("int"), [])]);
        Assert.Equal(should, typeNode);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould
            = new PlampException(PlampNativeExceptionInfo.Expected(nameof(CloseAngleBracket)),
                new(0, 8), new(0, 9));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
        Assert.Equal(3, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseMultipleGenerics()
    {
        var code = "Dict<int,string>";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("Dict"),
        [
            new TypeNode(new MemberNode("int"), []),
            new TypeNode(new MemberNode("string"), []),
        ]);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(5, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseExtendedTypeDeclaration()
    {
        var code = "std.int";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new MemberAccessNode(new MemberNode("std"), new TypeNode(new MemberNode("int"), []));
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(2, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseExtendedTypeDeclarationEndWithDot()
    {
        var code = "std.";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("std"), []);
        Assert.Equal(should, typeNode);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0), new(0, 3));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
        Assert.Equal(1, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseGenericWithDotBetween()
    {
        var code = "List.<int>";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), [new TypeNode(new MemberNode("int"), [])]);
        Assert.Equal(should, typeNode);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0), new(0, 4));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
        Assert.Equal(4, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyGeneric()
    {
        var code = "List<>";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), []);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(2, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseGenericWithoutOpenBracket()
    {
        var code = "List int>";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), []);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(0, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseKeywordTypeName()
    {
        var code = "var";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, result);
        Assert.Null(typeNode);
    }

    [Fact]
    public void ParseExtendedTypeDeclarationKeywordInBeginning()
    {
        var code = "var.int";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, result);
        Assert.Null(typeNode);
    }

    [Fact]
    public void ParseExtendedTypeDeclarationKeywordInEnding()
    {
        var code = "std.var";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("std"), []);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(0, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseGenericWithMissedGenericArg()
    {
        var code = "comparer<first,,comp>";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseType(transaction, out var typeNode);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("comparer"), 
            [
                new TypeNode(new MemberNode("first"), []),
                null,
                new TypeNode(new MemberNode("comp"), []),
            ]);
        Assert.Equal(should, typeNode);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(6, parser.TokenSequence.Position);
    }
}