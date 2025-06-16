using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Extensions.Ast.Comparers;
using plamp.Native.Parsing;
using Xunit;

namespace plamp.Native.Tests.Parser;

#pragma warning disable CS0618
public class TypeParsingTests
{
    public static readonly ExtendedRecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseSimpleType()
    {
        const string code = "int";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("int"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(0, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseSimpleGenericType()
    {
        const string code = "List<int>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), [new TypeNode(new MemberNode("int"), null)]);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(3, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseNotClosedGenericNonStrict()
    {
        const string code = "List<int";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context, false);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), [new TypeNode(new MemberNode("int"), null)]);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould
            = new PlampException(PlampNativeExceptionInfo.ParenExpressionIsNotClosed(),
                new(0, 4), new(0, 9),
                ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        Assert.Equal(3, context.TokenSequence.Position);
    }
    
    [Fact]
    public void ParseNotClosedGenericStrict()
    {
        const string code = "List<int";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Equal(0, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseMultipleGenerics()
    {
        const string code = "Dict<int,string>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("Dict"),
        [
            new TypeNode(new MemberNode("int"), null),
            new TypeNode(new MemberNode("string"), null),
        ]);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(5, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseExtendedTypeDeclaration()
    {
        var code = "std.int";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("std.int"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(2, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseExtendedTypeDeclarationEndWithDot()
    {
        var code = "std.";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("std"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0), new(0, 3),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        Assert.Equal(1, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseGenericWithDotBetween()
    {
        var code = "List.<int>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), [new TypeNode(new MemberNode("int"), null)]);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0), new(0, 4), 
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        Assert.Equal(4, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyGeneric()
    {
        var code = "List<>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould
            = new PlampException(PlampNativeExceptionInfo.InvalidGenericDefinition(), 
                new(0, 4), new(0, 5),
                ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        Assert.Equal(2, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseGenericWithoutOpenBracket()
    {
        const string code = "List int>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("List"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(0, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseKeywordTypeName()
    {
        const string code = "var";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, result);
        Assert.Null(typeNode);
    }

    [Fact]
    public void ParseExtendedTypeDeclarationKeywordInBeginning()
    {
        const string code = "var.int";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, result);
        Assert.Null(typeNode);
    }

    [Fact]
    public void ParseExtendedTypeDeclarationKeywordInEnding()
    {
        const string code = "std.var";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("std"), null);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.InvalidTypeName(), 
            new(0, 0), new(0, 3),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        
        Assert.Equal(1, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseGenericWithMissedGenericArg()
    {
        var code = "comparer<first,,comp>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.NotNull(typeNode);
        var should = new TypeNode(new MemberNode("comparer"), 
            [
                new TypeNode(new MemberNode("first"), null),
                null,
                new TypeNode(new MemberNode("comp"), null),
            ]);
        Assert.Equal(should, typeNode, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(6, context.TokenSequence.Position);
    }

    #region Symbol table

    [Fact]
    public void SimpleType()
    {
        const string code = "integral";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(2, symbolTable.Count);
        Assert.Contains(typeNode, symbolTable);
        var symbol = symbolTable[typeNode];
        Assert.Empty(symbol.Tokens);
        Assert.Single(symbol.Children);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void FullTypeDefinition()
    {
        const string code = "number.integral";
        var context = ParserTestHelper.GetContext(code); 
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(2, symbolTable.Count);
        Assert.Contains(typeNode, symbolTable);
        var symbol = symbolTable[typeNode];
        Assert.Empty(symbol.Tokens);
        Assert.Single(symbol.Children);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void TypeWithGenerics()
    {
        const string code = "t<strange>";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseType(transaction, out var typeNode, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(4, symbolTable.Count);
        Assert.Contains(typeNode, symbolTable);
        var symbol = symbolTable[typeNode];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    #endregion
}