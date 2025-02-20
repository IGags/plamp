using System;
using System.Linq;
using plamp.Ast;
using plamp.Native.Parsing;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete
namespace plamp.Native.Tests.Parser;

public class ParserHelperLogicTests
{
    #region AddExceptionToTheTokenRange

    
    /// <summary>
    /// Must add exception in ascending order
    /// </summary>
    [Theory]
    [InlineData("aaa", 0, 0)]
    [InlineData("aaa aaa", 0, 2)]
    [InlineData("a\na", 0, 2)]
    [InlineData("a->\na", 0, 3)]
    //Tokenizer implicitly add \n to the end of code
    [InlineData("", 0, 0)]
    public void TestAddExceptionToTokenRange(string code, int startToken, int endToken)
    {
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        parser.TokenSequence.Position = startToken;
        var start = parser.TokenSequence.Current();
        parser.TokenSequence.Position = endToken;
        var end = parser.TokenSequence.Current();
        parser.AddExceptionToTheTokenRange(start, end, 
            PlampNativeExceptionInfo.UnexpectedToken(null), transaction);
        transaction.Commit();

        Assert.Single(parser.TransactionSource.Exceptions);
        Assert.Equal(start.Start, parser.TransactionSource.Exceptions.First().StartPosition);
        Assert.Equal(end.End, parser.TransactionSource.Exceptions.First().EndPosition);
    }

    /// <summary>
    /// Exception can be caused by inverted order
    /// </summary>
    [Fact]
    public void TestAddExceptionToNegativeOrder()
    {
        var code = "1 ";
        var parser = new PlampNativeParser(code);
        parser.TokenSequence.Position = 0;
        var transaction = parser.TransactionSource.BeginTransaction();
        var start = parser.TokenSequence.Current();
        parser.TokenSequence.Position = 1;
        var end = parser.TokenSequence.Current();
        Assert.Throws<ArgumentException>(() =>
            parser.AddExceptionToTheTokenRange(end, start, 
                PlampNativeExceptionInfo.UnexpectedToken(null),
                transaction));
    }
    
    #endregion

    #region SkipLineBreak
    
    /// <summary>
    /// If parser encounters line break it should ignore next end of line
    /// and merge rest part of token sequence
    /// </summary>
    [Theory]
    [InlineData("", -1)]
    [InlineData("->\n", 1)]
    [InlineData(" ->\n", 2)]
    [InlineData("-> \n", 2)]
    [InlineData("->1", 0)]
    [InlineData("1->", -1)]
    public void SkipLineBreakTests(string code, int resultPos)
    {
        var parser = new PlampNativeParser(code);
        parser.SkipLineBreak();
        Assert.Equal(resultPos, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    #endregion

    #region AdvanceToEndOfLineOrRequested
    
    /// <summary>
    /// Parser should advance to requested token type
    /// or to end of line if it appears earlier
    /// </summary>
    [Theory]
    [InlineData("", 0)]
    [InlineData("  ", 2)]
    [InlineData("priv \n (", 2)]
    [InlineData("priv ->\n (", 5)]
    public void AdvanceToEndOfLineOrRequestedTests(string code, int resultPos)
    {
        var parser = new PlampNativeParser(code);
        parser.AdvanceToEndOfLineOrRequested<OpenParen>();
        Assert.Equal(resultPos, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Parser start from zero(not -1) position and should not move
    /// </summary>
    [Fact]
    public void AdvanceToEndOfLineOrRequestedIfOnRequested()
    {
        const string code = "((";
        var parser = new PlampNativeParser(code);
        parser.TokenSequence.Position = 0;
        parser.AdvanceToEndOfLineOrRequested<OpenParen>();
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Parser starts from 1 pos, but requested token has 0 position
    /// </summary>
    [Fact]
    public void AdvanceToEndOfLineOrRequestedIdTokenBefore()
    {
        const string code = "( \n";
        var parser = new PlampNativeParser(code);
        parser.TokenSequence.Position = 1;
        parser.AdvanceToEndOfLineOrRequested<OpenParen>();
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Corner case. Parser shouldn't advance to whitespace
    /// </summary>
    [Fact]
    public void ExceptAdvanceToWhiteSpaceOrEndOfLine()
    {
        const string code = " ";
        var parser = new PlampNativeParser(code);
        Assert.Throws<Exception>(() => parser.AdvanceToEndOfLineOrRequested<WhiteSpace>());
    }
    
    #endregion

    #region TryConsumeNextNonWhiteSpace
    
    /// <summary>
    /// Parser should return false
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceOnEmptyString()
    {
        const string code = "";
        var parser = new PlampNativeParser(code);
        var falsePredicateExecuted = false;
        var result = parser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// Returns true and don't call false action
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForMatchToken()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var falsePredicateExecuted = false;
        var result = parser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token);
        Assert.True(result);
        Assert.Equal(typeof(OpenParen), token.GetType());
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.False(falsePredicateExecuted);
    }

    /// <summary>
    /// Next non-whitespace has different type
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForTypeMismatch()
    {
        const string code = ")";
        var parser = new PlampNativeParser(code);
        var falsePredicateExecuted = false;
        var result = parser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// Next non-whitespace has same type but suddenly predicate returns false
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForPredicateMismatch()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var falsePredicateExecuted = false;
        var result = parser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => false, _ => falsePredicateExecuted = true, out var token);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// From parser position to non-whitespace token only whitespaces
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceWithWhiteSpaceBetween()
    {
        const string code = " (";
        var parser = new PlampNativeParser(code);
        var falsePredicateExecuted = false;
        var result = parser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token);
        Assert.True(result);
        Assert.Equal(typeof(OpenParen), token.GetType());
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.False(falsePredicateExecuted);
    }

    /// <summary>
    /// Code has whitespace only
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceWithOnlyWhiteSpace()
    {
        const string code = "    ";
        var parser = new PlampNativeParser(code);
        var falsePredicateExecuted = false;
        var result = parser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// Method calls with whitespace as generic
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForWhiteSpace()
    {
        const string code = "";
        var parser = new PlampNativeParser(code);
        Assert.Throws<Exception>(() => parser.TryConsumeNextNonWhiteSpace<WhiteSpace>(
            _ => true, _ => { }, out _));
    }
    
    #endregion

    #region TryParseCommaSeparated

    /// <summary>
    /// Parsing of empty sequence returns fail
    /// </summary>
    [Fact]
    public void TryParseCommaSeparatedEmpty()
    {
        const string code = "";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Single(result);
        Assert.Null(result[0]);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Should complete with one element and success
    /// </summary>
    [Fact]
    public void TryParseCommaSeparatedOneMatchCase()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.Single(result);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedOneMismatchCase()
    {
        const string code = ")";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Single(result);
        Assert.Null(result[0]);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedTwoMatchCases()
    {
        const string code = "(,(";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.Equal(2, result.Count);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Equal(typeof(OpenParen), result[1].GetType());
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedMatchAndMismatch()
    {
        const string code = "(,)";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Equal(2, result.Count);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Null(result[1]);
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedMismatchAndMatch()
    {
        const string code = "),(";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Single(result);
        Assert.Null(result[0]);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedWithWrongSplitter()
    {
        const string code = "(-(";
        var parser = new PlampNativeParser(code);
        var res = parser.TryParseCommaSeparated(
            TryConsumeOpenParen(parser), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.Single(result);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
    
    /// <summary>
    /// Internal function that consumes only open parens
    /// </summary>
    private PlampNativeParser.TryParseInternal<OpenParen> TryConsumeOpenParen(PlampNativeParser parser)
    {
        PlampNativeParser.ExpressionParsingResult Internal(out OpenParen paren)
        {
            var res = parser.TryConsumeNextNonWhiteSpace(_ => true, _ => { }, out paren);
            return res
                ? PlampNativeParser.ExpressionParsingResult.Success
                : PlampNativeParser.ExpressionParsingResult.FailedNeedRollback;
        }

        return Internal;
    }
    #endregion

    #region TryParseInParen

    [Fact]
    public void TryParseInParenEmpty()
    {
        const string code = "";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = parser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(parser), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedPass, res);
        Assert.False(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithoutOpenParen()
    {
        const string code = "-)";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = parser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(parser), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedPass, res);
        Assert.False(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithEmptyParens()
    {
        const string code = "()";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = parser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(parser), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, res);
        Assert.True(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithoutCloseParen()
    {
        const string code = "(-";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = parser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(parser), (_, _) =>
            {
                emptyCaseInvoked = true;
                return null;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.False(emptyCaseInvoked);
        Assert.Equal(OperatorEnum.Minus, @operator.Operator);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.ParenExpressionIsNotClosed(),
            new FilePosition(0, 0), new FilePosition(0, 3));
        //CRLF end of line has two characters
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void TryParseInParenValid()
    {
        const string code = "(-)";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = parser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(parser), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.False(emptyCaseInvoked);
        Assert.Equal(OperatorEnum.Minus, @operator.Operator);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithInvalidInternalExpression()
    {
        const string code = "(word)";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = parser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(parser), (_, _) =>
            {
                emptyCaseInvoked = true;
                return null;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.False(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new FilePosition(0, 0), new FilePosition(0, 5));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }
    
    /// <summary>
    /// Internal function that returns operators.
    /// I know that I have same function above, but I don't want to split functions between regions
    /// </summary>
    private PlampNativeParser.TryParseInternal<OperatorToken> ConsumeOperator(PlampNativeParser parser)
    {
        PlampNativeParser.ExpressionParsingResult Internal(out OperatorToken @operator)
        {
            var res = parser.TryConsumeNextNonWhiteSpace(_ => true, _ => { }, out @operator);
            return res
                ? PlampNativeParser.ExpressionParsingResult.Success
                : PlampNativeParser.ExpressionParsingResult.FailedNeedRollback;
        }

        return Internal;
    }

    #endregion

    //TODO: Duplicated logic
    #region AdvanceToRequestedTokenWithException

    [Fact]
    public void AdvanceToRequestedTokenWithExceptionOnEndOfLine()
    {
        const string code = "\n(";
        var parser = new PlampNativeParser(code);
        parser.TokenSequence.Position = 0;
        var transaction = parser.TransactionSource.BeginTransaction();
        parser.AdvanceToRequestedTokenWithException<OpenParen>(transaction);
        transaction.Commit();
        
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Has strange behaviour because of
    /// (means first we call tryConsume then call this method.
    /// I made this because I don't want to create complicated signature)
    /// </summary>
    [Fact]
    public void AdvanceToRequestedTokenWithExceptionIfNextIsRequested()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        parser.AdvanceToRequestedTokenWithException<OpenParen>(transaction);
        transaction.Commit();
        
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var expectedException = new PlampException(PlampNativeExceptionInfo.Expected(nameof(OpenParen)),
            new FilePosition(0, 0), new FilePosition(0, 0));
        
        Assert.Equal(expectedException, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void AdvanceToRequestedTokenWithExceptionIfNextIsNotRequested()
    {
        const string code = "-(";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        parser.AdvanceToRequestedTokenWithException<OpenParen>(transaction);
        transaction.Commit();
        
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var expectedException = new PlampException(PlampNativeExceptionInfo.Expected(nameof(OpenParen)),
            new FilePosition(0, 0), new FilePosition(0, 1));
        
        Assert.Equal(expectedException, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void AdvanceToRequestedTokenWithExceptionIfNextIsEndOfLine()
    {
        const string code = "\n(";
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        parser.AdvanceToRequestedTokenWithException<OpenParen>(transaction);
        transaction.Commit();
        
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
    
    #endregion

    #region TryConsumeNext

    [Fact]
    public void TryConsumeNextOnEmpty()
    {
        const string code = "";
        var parser = new PlampNativeParser(code);
        var falsePredicateCalled = false;
        var res = parser.TryConsumeNext<OpenParen>(
            _ => true, _ => falsePredicateCalled = true, out var result);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeNextWhiteSpace()
    {
        const string code = " ";
        var parser = new PlampNativeParser(code);
        var falsePredicateCalled = false;
        var res = parser.TryConsumeNext<WhiteSpace>(
            _ => true, _ => falsePredicateCalled = true, out var result);
        Assert.True(res);
        Assert.Equal(typeof(WhiteSpace), result.GetType());
        Assert.False(falsePredicateCalled);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeNextNonWhiteSpace()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var falsePredicateCalled = false;
        var res = parser.TryConsumeNext<OpenParen>(
            _ => true, _ => falsePredicateCalled = true, out var result);
        Assert.True(res);
        Assert.Equal(typeof(OpenParen), result.GetType());
        Assert.False(falsePredicateCalled);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeNextWhiteSpaceFail()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var falsePredicateCalled = false;
        var res = parser.TryConsumeNext<WhiteSpace>(
            _ => true, _ => falsePredicateCalled = true, out var result);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void TryConsumeNextNonWhiteSpaceFail()
    {
        const string code = " ";
        var parser = new PlampNativeParser(code);
        var falsePredicateCalled = false;
        var res = parser.TryConsumeNext<WhiteSpace>(
            _ => false, _ => falsePredicateCalled = true, out var result);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeWithFalsePredicate()
    {
        const string code = "(";
        var parser = new PlampNativeParser(code);
        var falsePredicateCalled = false;
        var res = parser.TryConsumeNext<WhiteSpace>(
            _ => false, _ => falsePredicateCalled = true, out var result);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    #endregion
}