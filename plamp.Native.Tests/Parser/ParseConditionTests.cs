using System.Linq;
using System.Reflection.Emit;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseConditionTests
{
    [Fact]
    public void ParseValidSingleLineCondition()
    {
        const string code = "if(i==7)k++";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new EqualNode(
                        new MemberNode("i"),
                        new ConstNode(7, typeof(int))),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("k"))
                    ])),
                [],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(8, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidMultiLineCondition()
    {
        const string code = """
                            if(i==3)
                                i++
                                expose()
                                return 0
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new EqualNode(
                        new MemberNode("i"),
                        new ConstNode(3, typeof(int))),
                    new BodyNode(
                        [
                            new PostfixIncrementNode(new MemberNode("i")),
                            new CallNode(new MemberNode("expose"), []),
                            new ReturnNode(new ConstNode(0, typeof(int)))
                        ]
                    )),
                [],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(20, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseIfConditionWithoutPredicate()
    {
        const string code = "if()i++";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    null,
                    new BodyNode(
                        [
                            new PostfixIncrementNode(
                                new MemberNode("i"))
                        ])),
                [],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould =
            new PlampException(PlampNativeExceptionInfo.EmptyConditionPredicate(), 
                new(0, 2), new(0, 3));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
    }

    [Fact]
    public void ParseIfConditionWithoutClosingParenSingleLine()
    {
        const string code = "if(r return 1";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("r"),
                    new BodyNode(
                        [
                        ])),
                [],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(7, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.ParenExpressionIsNotClosed(), 
            new(0, 2), new(0, 14));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
    }

    [Fact]
    public void ParseIfConditionWithoutClosingParenMultiLine()
    {
        const string code = """
                            if(r
                                return 1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("r"),
                    new BodyNode(
                    [
                        new ReturnNode(new ConstNode(1, typeof(int)))
                    ])),
                [],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(8, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.ParenExpressionIsNotClosed(), 
            new(0, 2), new(0, 5));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
    }

    [Fact]
    public void ParseIfOpenParenIsMissing()
    {
        const string code = "if r) return 1";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(8, parser.TokenSequence.Position);
        Assert.Equal(2, parser.TransactionSource.Exceptions.Count);
        var exceptionShould1 =
            new PlampException(PlampNativeExceptionInfo.MissingConditionPredicate(), 
                new(0, 0), new(0, 1));
        Assert.Equal(exceptionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 =
            new PlampException(PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
                new(0, 2), new(0, 15));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseIfElseSingleLineSuccess()
    {
        const string code = """
                            if(true) return 1
                            else i++
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new ConstNode(true, typeof(bool)),
                    new BodyNode(
                    [
                        new ReturnNode(
                            new ConstNode(1, typeof(int)))
                    ])),
                [],
                new BodyNode(
                [
                    new PostfixIncrementNode(
                        new MemberNode("i"))
                ]));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(13, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseIfElseMultiLineSuccess()
    {
        const string code = """
                            if(true) i++
                            else
                                i--
                                --i
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new ConstNode(true, typeof(bool)),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("i"))
                    ])),
                [],
                new BodyNode(
                [
                    new PostfixDecrementNode(
                        new MemberNode("i")),
                    new PrefixDecrementNode(
                        new MemberNode("i"))
                ]));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(17, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseElseOnly()
    {
        const string code = """
                            else fun
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedPass, result);
        Assert.Null(expression);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseElseWithoutBody()
    {
        const string code = """
                            if(i) a()
                            else
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("a"), [])
                    ])),
                [],
                new BodyNode(
                    []));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(10, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidSingleLineElifClause()
    {
        const string code = """
                            if(i) a()
                            elif(!i) b()
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("a"),
                            [])
                    ])),
                [
                    new ClauseNode(
                        new NotNode(
                            new MemberNode("i")),
                        new BodyNode(
                        [
                            new CallNode(
                                new MemberNode("b"),
                                [])
                        ]))
                ],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(18, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidMultiLineElifClause()
    {
        const string code = """
                            if(i) a()
                            elif(!i)
                                var d = b() + c()
                                return d
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("a"),
                            [])
                    ])),
                [
                    new ClauseNode(
                        new NotNode(
                            new MemberNode("i")),
                        new BodyNode(
                        [
                            new AssignNode(
                                new VariableDefinitionNode(
                                    null,
                                    new MemberNode("d")),
                                new PlusNode(
                                    new CallNode(
                                        new MemberNode("b"), 
                                        []),
                                    new CallNode(
                                        new MemberNode("c"),
                                        []))),
                            new ReturnNode(
                                new MemberNode("d"))
                        ]))
                ],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(36, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseElifClauseWithEmptyBody()
    {
        const string code = """
                            if(i) --t
                            elif(t)
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new PrefixDecrementNode(
                            new MemberNode("t"))
                    ])),
                [
                    new ClauseNode(
                        new MemberNode("t"),
                        new BodyNode(
                            []))
                ],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(12, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseElifMissingOpenParen()
    {
        const string code = """
                   if(i) t()
                   elif !i)
                       return !t()
                   """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("t"),
                            [])
                    ])),
                [],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(22, parser.TokenSequence.Position);
        Assert.Equal(3, parser.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.MissingConditionPredicate(), 
            new(1, 0), new(1, 3));
        Assert.Equal(exceptionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(1, 4), new(1, 9));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
        var exceptionShould3 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(2, 4), new(2, 16));
        Assert.Equal(exceptionShould3, parser.TransactionSource.Exceptions[2]);
    }

    [Fact]
    public void ParseManyElifClause()
    {
        const string code = """
                            if(i) c++
                            elif(!i)
                                print("hi")
                                c--
                            elif(false) return 1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("c"))
                    ])),
                [
                    new ClauseNode(
                        new NotNode(
                            new MemberNode("i")),
                        new BodyNode(
                        [
                            new CallNode(
                                new MemberNode("print"),
                                [
                                    new ConstNode("hi", typeof(string))
                                ]),
                            new PostfixDecrementNode(
                                new MemberNode("c"))
                        ])),
                    new ClauseNode(
                        new ConstNode(false, typeof(bool)),
                        new BodyNode(
                        [
                            new ReturnNode(
                                new ConstNode(1, typeof(int)))
                        ]))
                ],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(32, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseManyElifClausesFirstEmptyBody()
    {
        const string code = """
                            if(i) c++
                            elif(!i)
                            elif(false) return 1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("c"))
                    ])),
                [
                    new ClauseNode(
                        new NotNode(
                            new MemberNode("i")),
                        new BodyNode([])),
                    new ClauseNode(
                        new ConstNode(false, typeof(bool)),
                        new BodyNode(
                        [
                            new ReturnNode(
                                new ConstNode(1, typeof(int)))
                        ]))
                ],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(22, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseManyElifClausesSecondEmptyBodyWithElse()
    {
        const string code = """
                            if(i) c++
                            elif(!i) return 1
                            elif(false)
                            else return 2
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new MemberNode("i"),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("c"))
                    ])),
                [
                    new ClauseNode(
                        new NotNode(
                            new MemberNode("i")),
                        new BodyNode(
                        [
                            new ReturnNode(
                            new ConstNode(1, typeof(int)))
                        ])),
                    new ClauseNode(
                        new ConstNode(false, typeof(bool)),
                        new BodyNode([]))
                ],
                new BodyNode(
                [
                    new ReturnNode(
                        new ConstNode(2, typeof(int)))
                ]));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(28, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseManyElifClausesFirstWithoutOpenParen()
    {
        const string code = """
                            if(true) c++
                            elif
                                c()
                                c()
                            elif(false) return null
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new ConstNode(true, typeof(bool)),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("c"))
                    ])),
                [
                    new ClauseNode(
                        new ConstNode(false, typeof(bool)),
                        new BodyNode(
                        [
                            new ReturnNode(
                                new ConstNode(null, null))
                        ]))
                ],
                null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(28, parser.TokenSequence.Position);
        Assert.Equal(3, parser.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.MissingConditionPredicate(), 
            new(1, 0), new(1, 3));
        Assert.Equal(exceptionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(2, 4), new(2, 8));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
        var exceptionShould3 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(3, 4), new(3, 8));
        Assert.Equal(exceptionShould3, parser.TransactionSource.Exceptions[2]);
    }

    [Fact]
    public void ParseManyElifClauseLastWithoutOpenParenWithElse()
    {
        const string code = """
                            if(true) c++
                            elif(false) return null
                            elif
                                c()
                                c()
                            else
                                w()
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new ConstNode(true, typeof(bool)),
                    new BodyNode(
                    [
                        new PostfixIncrementNode(
                            new MemberNode("c"))
                    ])),
                [
                    new ClauseNode(
                        new ConstNode(false, typeof(bool)),
                        new BodyNode(
                        [
                            new ReturnNode(
                                new ConstNode(null, null))
                        ]))
                ],
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("w"),
                        [])
                ]));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(35, parser.TokenSequence.Position);
        Assert.Equal(3, parser.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.MissingConditionPredicate(), 
            new(2, 0), new(2, 3));
        Assert.Equal(exceptionShould1, parser.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(3, 4), new(3, 8));
        Assert.Equal(exceptionShould2, parser.TransactionSource.Exceptions[1]);
        var exceptionShould3 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(4, 4), new(4, 8));
        Assert.Equal(exceptionShould3, parser.TransactionSource.Exceptions[2]);
    }
}