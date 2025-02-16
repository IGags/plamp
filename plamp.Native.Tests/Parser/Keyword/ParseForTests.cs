using plamp.Ast;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.Unary;
using plamp.Ast.NodeComparers;
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
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new ConstNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new ConstNode(10, typeof(int))),
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
        Assert.Equal(20, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCorrectForMultiLine()
    {
        const string code = """
                            for(var i=0,i<10,i++)
                                print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new ConstNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new ConstNode(10, typeof(int))),
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
        Assert.Equal(21, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseWithoutCloseParenSingleLine()
    {
        const string code = """
                            for(var i=0,i<10,i++ print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new ConstNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new ConstNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(19, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new(0, 3), new(0, 28));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseForWithoutOpenParenSingleLine()
    {
        const string code = """
                            for var i=0,i<10,i++) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(20, parser.TokenSequence.Position);
        Assert.Equal(2, parser.TransactionSource.Exceptions.Count);
        var expressionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidForHeader(), new(0, 0), new(0, 2));
        Assert.Equal(expressionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 3), new(0, 31));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseForWithoutSecondDelimiter()
    {
        const string code = """
                            for(var i=0,i<10 i++) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new ConstNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new ConstNode(10, typeof(int))),
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
        Assert.Equal(20, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(Comma)),
            new(0, 16), new(0, 16));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseWhileThroughFor()
    {
        const string code = """
                            for(,,) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
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
        Assert.Equal(10, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForWithoutBody()
    {
        const string code = """
                            for(var i=0,i<10,i++)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ForNode(
                new AssignNode(
                    new VariableDefinitionNode(
                        null,
                        new MemberNode("i")),
                    new ConstNode(0, typeof(int))),
                new LessNode(
                    new MemberNode("i"),
                    new ConstNode(10, typeof(int))),
                new PostfixIncrementNode(
                    new MemberNode("i")),
                new BodyNode(
                []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(15, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachSingleLine()
    {
        const string code = """
                            for(var i in col) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
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
        Assert.Equal(15, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachMultiLine()
    {
        const string code = """
                            for(var i in col)
                                print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
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
        Assert.Equal(16, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachWithoutCloseParenSingleLine()
    {
        const string code = """
                            for(var i in col print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
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
        Assert.Equal(14, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new(0, 3), new(0, 24));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseForeachWithoutOpenParenSingleLine()
    {
        const string code = """
                            for var i in col) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(15, parser.TokenSequence.Position);
        Assert.Equal(2, parser.TransactionSource.Exceptions.Count);
        var expressionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidForHeader(), new(0, 0), new(0, 2));
        Assert.Equal(expressionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 3), new(0, 27));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseWithoutInKeyword()
    {
        const string code = """
                            for(var i col) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(13, parser.TokenSequence.Position);
        Assert.Equal(2, parser.TransactionSource.Exceptions.Count);
        var expressionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidForHeader(), new(0, 0), new(0, 2));
        Assert.Equal(expressionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 14), new(0, 24));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseForeachWithoutFirstExpression()
    {
        const string code = """
                            for(in col) print(i)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
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
        Assert.Equal(11, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseForeachWithoutBody()
    {
        const string code = """
                            for(var i in col)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
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
        Assert.Equal(10, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
}