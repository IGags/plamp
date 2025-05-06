using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Extensions;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.Keyword;

public class ParseForTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseCorrectForSingleLine()
    {
        const string code = """
                            for(var i=0,i<10,i++) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new LiteralNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new LiteralNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(20, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCorrectForMultiLine()
    {
        const string code = """
                            for(var i=0,i<10,i++)
                                print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new LiteralNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new LiteralNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(21, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseWithoutCloseParenSingleLine()
    {
        const string code = """
                            for(var i=0,i<10,i++ print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new LiteralNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new LiteralNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(19, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new(0, 3), new(0, 28),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseForWithoutOpenParenSingleLine()
    {
        const string code = """
                            for var i=0,i<10,i++) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(20, context.TokenSequence.Position);
        Assert.Equal(2, context.TransactionSource.Exceptions.Count);
        var expressionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidForHeader(), new(0, 0), new(0, 2),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(expressionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 3), new(0, 31),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseForWithoutSecondDelimiter()
    {
        const string code = """
                            for(var i=0,i<10 i++) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new LiteralNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new LiteralNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(20, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(Comma)),
            new(0, 16), new(0, 16),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseWhileThroughFor()
    {
        const string code = """
                            for(,,) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                null,
                null,
                null,
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(10, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForWithoutBody()
    {
        const string code = """
                            for(var i=0,i<10,i++)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new LiteralNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new LiteralNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(15, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachSingleLine()
    {
        const string code = """
                            for(var i in col) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForeachNode(
                new VariableDefinitionNode(
                    null,
                    new MemberNode("i")),
                new MemberNode("col"),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(15, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachMultiLine()
    {
        const string code = """
                            for(var i in col)
                                print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForeachNode(
                new VariableDefinitionNode(
                    null,
                    new MemberNode("i")),
                new MemberNode("col"),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(16, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachWithoutCloseParenSingleLine()
    {
        const string code = """
                            for(var i in col print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForeachNode(
                new VariableDefinitionNode(
                    null,
                    new MemberNode("i")),
                new VariableDefinitionNode(
                    new TypeNode(
                        new MemberNode("col"), 
                        null),
                    new MemberNode("print")),
                new BodyNode(
                []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(14, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new(0, 3), new(0, 24),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseForeachWithoutOpenParenSingleLine()
    {
        const string code = """
                            for var i in col) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(15, context.TokenSequence.Position);
        Assert.Equal(2, context.TransactionSource.Exceptions.Count);
        var expressionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidForHeader(), new(0, 0), new(0, 2),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(expressionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 3), new(0, 27),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseWithoutInKeyword()
    {
        const string code = """
                            for(var i col) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(13, context.TokenSequence.Position);
        Assert.Equal(2, context.TransactionSource.Exceptions.Count);
        var expressionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidForHeader(), 
            new(0, 0), new(0, 2),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(expressionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 14), new(0, 24),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseForeachWithoutFirstExpression()
    {
        const string code = """
                            for(in col) print(i)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForeachNode(
                null,
                new MemberNode("col"),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new MemberNode("i")
                        ])
                ]));
        
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(11, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachWithoutBody()
    {
        const string code = """
                            for(var i in col)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForeachNode(
                new VariableDefinitionNode(
                    null,
                    new MemberNode("i")),
                new MemberNode("col"),
                new BodyNode(
                []));
        
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(10, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #region SymbolTable

    [Fact]
    public void SymbolTableForSingleLine()
    {
        const string code = """
                            for(int i=0,i<c,i++) print(i)
                            """;

        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(16, symbolTable.Count);
        var first = symbolTable[expression];
        Assert.Single(first.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], first.Tokens[0]);
        Assert.Equal(4, first.Children.Count);
        foreach (var child in first.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void SymbolTableForeachSingleLine()
    {
        const string code = """
                            for(var i in c) print(i)
                            """;

        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(8, symbolTable.Count);
        var first = symbolTable[expression];
        Assert.Single(first.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], first.Tokens[0]);
        Assert.Equal(3, first.Children.Count);
        foreach (var child in first.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    #endregion
}